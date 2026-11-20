const fs = require('node:fs');

const folders = [
  'Beethoven_FurElise', 'Pachelbel_CanonInD', 'Bach_PreludeInC',
  'Mozart_SonataK545_Rondo', 'Chopin_PreludeInEMinor',
];
const tiers = {
  Therapy: { gap: 2.65, lookAhead: .52, holdMin: .82, holdMax: 2.1, holdSpace: 8, range: 0, speed: 0 },
  Easy:    { gap: 1.55, lookAhead: .28, holdMin: .68, holdMax: 1.55, holdSpace: 9, range: .75, speed: .30 },
  Medium:  { gap: .97, lookAhead: .16, holdMin: .53, holdMax: 1.2, holdSpace: 8, range: .94, speed: .45 },
  Hard:    { gap: .28, lookAhead: .075, holdMin: .45, holdMax: .9, holdSpace: 7, range: 1, speed: .5 },
};
const round = value => Math.round(value * 1e6) / 1e6;
const clamp = (value, low, high) => Math.max(low, Math.min(high, value));
const read = file => JSON.parse(fs.readFileSync(file, 'utf8'));
const write = (file, value) => fs.writeFileSync(file, JSON.stringify(value, null, 2) + '\n');
const beatDistance = (beat, unit) => Math.abs(beat - Math.round(beat / unit) * unit);

function groupAttacks(notes) {
  const attacks = [];
  for (const note of notes) {
    const last = attacks.at(-1);
    if (!last || note.time - last.time > .018) {
      attacks.push({ ...note, chordSize: 1 });
      continue;
    }
    last.pitch = Math.max(last.pitch, note.pitch);
    last.end = Math.max(last.end, note.end);
    last.velocity = Math.max(last.velocity, note.velocity);
    last.chordSize++;
  }
  return attacks;
}

function audioAttacks(audio) {
  // Rising RMS finds the body of a note. The difference signal also responds
  // to a new attack over piano tones that are still ringing out.
  const envelope = audio.energy.map(([rms, edge]) => Math.log1p(140 * rms) + .53 * Math.log1p(650 * edge));
  const flux = envelope.map((value, index) => Math.max(0, value -
    (envelope[Math.max(0, index - 2)] + envelope[Math.max(0, index - 4)]) / 2));
  const radius = Math.round(audio.rate * .075);
  const salience = flux.map((value, i) => {
    let nearby = .12;
    for (let j = Math.max(0, i - radius); j <= Math.min(flux.length - 1, i + radius); j++)
      nearby = Math.max(nearby, flux[j]);
    return value / nearby;
  });
  const strengthAt = time => {
    const index = Math.round(time * audio.rate);
    return Math.max(salience[index] ?? 0, (salience[index - 1] ?? 0) * .85, (salience[index + 1] ?? 0) * .85);
  };
  return { salience, strengthAt };
}

function align(midi, audio, metadata, musicEnd) {
  const { strengthAt, salience } = audioAttacks(audio);
  const attacks = groupAttacks(midi.notes);
  const priorOffset = metadata.alignment.offsetSeconds;
  const priorScale = metadata.alignment.scale;
  const reference = attacks.filter((n, i) => i % 2 === 0 && n.time * priorScale + priorOffset < musicEnd - .2);
  let best = { score: -Infinity };
  for (let rateStep = -6; rateStep <= 6; rateStep++) {
    const scale = priorScale + rateStep * .0001;
    for (let shift = -60; shift <= 60; shift += 3) {
      const offset = priorOffset + shift / 1000;
      const score = reference.reduce((sum, note) => sum +
        strengthAt(note.time * scale + offset) * (.45 + note.velocity / 160), 0);
      if (score > best.score) best = { score, scale, offset };
    }
  }

  const corrections = [];
  for (let center = 0; center <= musicEnd + 8; center += 8) {
    const anchors = reference.filter(n => Math.abs(n.time * best.scale + best.offset - center) <= 6);
    let shiftMs = 0, quality = -Infinity;
    if (anchors.length >= 8) {
      for (let shift = -24; shift <= 24; shift += 4) {
        const score = anchors.reduce((sum, n) => sum + strengthAt(n.time * best.scale + best.offset + shift / 1000), 0)
          - Math.abs(shift) * anchors.length * .0018;
        if (score > quality) { quality = score; shiftMs = shift; }
      }
    }
    corrections.push(shiftMs / 1000);
  }

  const result = [];
  let totalCorrection = 0;
  for (const n of attacks) {
    const mapped = n.time * best.scale + best.offset;
    if (mapped < 0 || mapped >= musicEnd) continue;
    const segment = Math.floor(mapped / 8);
    const mix = mapped / 8 - segment;
    const center = mapped + corrections[segment] * (1 - mix) + corrections[segment + 1] * mix;
    const frame = Math.round(center * audio.rate);
    const radius = Math.round(.024 * audio.rate);
    let strongest = 0, onset = center;
    for (let j = frame - radius; j <= frame + radius; j++) {
      const weighted = (salience[j] ?? 0) - .035 * Math.abs(j - frame);
      if (weighted > strongest) { strongest = weighted; onset = j / audio.rate; }
    }
    // Keep the MIDI map in quiet passages with no distinct audio transient.
    if (strongest < .27) onset = center;
    if (onset < 0 || onset >= musicEnd) continue;
    totalCorrection += Math.abs(onset - mapped);
    result.push({ ...n, time: onset, release: onset + Math.max(0, n.end - n.time) * best.scale,
      strength: strengthAt(onset) });
  }
  result.sort((a, b) => a.time - b.time);
  return { attacks: result, alignment: {
    offsetSeconds: round(best.offset), scale: round(best.scale),
    localAdjustmentMeanMs: round(1000 * totalCorrection / result.length),
    method: 'MIDI tempo map fitted to recorded piano attacks, with local timing adjustments',
  }};
}

function chooseNotes(attacks, tier, settings) {
  const chosen = [];
  let index = 0, nextTime = 0, quickGaps = 0;
  while (index < attacks.length) {
    const first = attacks[index];
    if (first.time + .000001 < nextTime) { index++; continue; }
    const phrase = Math.floor((first.beat + .07) / 8);
    const beatInPhrase = first.beat - phrase * 8;
    // Leave the end of each phrase open, with more room after alternating phrases.
    const restStart = phrase % 2 === 0 ? 7.08 : 6.82;
    if (chosen.length > 0 && beatInPhrase > restStart) { index++; continue; }
    let best = index, priority = -Infinity;
    for (let cursor = index; cursor < attacks.length; cursor++) {
      const note = attacks[cursor];
      const delay = note.time - first.time;
      if (delay > settings.lookAhead || Math.floor((note.beat + .07) / 8) !== phrase ||
          note.beat - phrase * 8 > restStart) break;
      const strongBeat = beatDistance(note.beat, 1) < .09;
      const halfBeat = beatDistance(note.beat, .5) < .085;
      const accent = (strongBeat ? 1.05 : halfBeat ? .45 : 0) + note.strength * .65
        + note.velocity / 160 + Math.min(note.chordSize - 1, 3) * .10
        + Math.min(1, note.release - note.time) * .35;
      const score = accent - delay / settings.lookAhead * 1.35;
      if (score > priority) { best = cursor; priority = score; }
    }
    const note = attacks[best];
    index = best + 1;
    const gap = note.time - (chosen.at(-1)?.time ?? -10);
    quickGaps = gap < .45 ? quickGaps + 1 : 0;
    chosen.push(note);
    const lighterHardPhrase = tier === 'Hard' && phrase % 3 === 1;
    const floor = lighterHardPhrase ? .48 : settings.gap;
    nextTime = note.time + (tier === 'Hard' && quickGaps >= 3 ? .76 : floor);
  }
  return chosen;
}

function chart(attacks, tier, settings, meta, musicEnd) {
  const chosen = chooseNotes(attacks, tier, settings);
  const pitches = attacks.map(note => note.pitch).sort((a, b) => a - b);
  const [low, high] = [.08, .92].map(percentile => pitches[Math.floor(pitches.length * percentile)]);
  const recovery = tier === 'Therapy' ? .65 : tier === 'Easy' ? .42 : .32;
  let previousHold = -Infinity, height = .35;
  const notes = chosen.map((note, index) => {
    const before = chosen[index - 1];
    const after = chosen[index + 1];
    const gap = note.time - (before?.time ?? -2);
    if (tier !== 'Therapy') {
      const contour = clamp((note.pitch - low) / Math.max(1, high - low), 0, 1);
      const target = Math.round((.12 + contour * settings.range * .86) * 16) / 16;
      const step = Math.min(.42, Math.max(.0625, gap * settings.speed));
      height = clamp(target, Math.max(0, height - step), Math.min(1, height + step));
    }
    const naturalDuration = note.release - note.time;
    const available = Math.min(after ? after.time - recovery : musicEnd, musicEnd) - note.time;
    const holdDuration = Math.min(naturalDuration, settings.holdMax, available);
    const accent = beatDistance(note.beat, 1) < .12 || note.strength >= .48 || note.chordSize >= 2;
    const hold = accent && holdDuration >= settings.holdMin && note.time - previousHold >= settings.holdSpace
      ? holdDuration : 0;
    if (hold > 0) previousHold = note.time;
    const target = tier === 'Therapy' ? .5 : height;
    return {
      beat: round((note.time + meta.leadIn) * meta.bpm / 60),
      lane: Math.round(target * 2),
      useCustomHeight: tier !== 'Therapy',
      customHeight: round(target),
      holdBeats: round(hold * meta.bpm / 60),
    };
  });
  return {
    description: meta.title + ' — ' + tier + '. Recording-aligned musical attacks, phrase breaks and sustain-based holds.',
    durationSeconds: round(meta.audioDuration + meta.leadIn),
    notes,
  };
}

for (const folder of folders) {
  const dir = 'Assets/Music/Classical/' + folder;
  const file = dir + '/ChartingNotes.json';
  const meta = read(file);
  const midi = read('Temp/ClassicalAnalysis/' + folder + '.json');
  const audio = read('Temp/ClassicalAnalysis/' + folder + '-transients.json');
  const edit = fs.existsSync(dir + '/AudioEdit.json') ? read(dir + '/AudioEdit.json') : null;
  const musicEnd = Math.min(audio.duration - .7, edit ? edit.sourceEndSeconds - edit.fadeOutSeconds - .35 : audio.duration);
  // Use the same coarse seed on reruns so refinement does not accumulate.
  meta.alignmentSeed ??= { offsetSeconds: meta.alignment.offsetSeconds, scale: meta.alignment.scale };
  const aligned = align(midi, audio, { alignment: meta.alignmentSeed }, musicEnd);
  const charts = Object.fromEntries(Object.entries(tiers).map(([tier, settings]) => {
    // The Rondo's staccato melody calls for a shorter Therapy hold.
    const profile = tier === 'Therapy' && folder === 'Mozart_SonataK545_Rondo'
      ? { ...settings, holdMin: .58 } : settings;
    return [tier, chart(aligned.attacks, tier, profile, meta, musicEnd)];
  }));
  if (!(charts.Easy.notes.length < charts.Medium.notes.length && charts.Medium.notes.length < charts.Hard.notes.length))
    throw Error(folder + ': difficulty density failed');
  for (const [tier, value] of Object.entries(charts))
    if (!value.notes.some(n => n.holdBeats > 0)) throw Error(folder + ': ' + tier + ' has no sustained notes');
  for (const [tier, value] of Object.entries(charts)) write(dir + '/Charts/' + tier + '.json', value);
  meta.alignment = aligned.alignment;
  meta.freeDifficultyVersion = 3;
  meta.freeDifficultyDesign = {
    Easy: 'Spaced musical accents and gentle height transitions',
    Medium: 'More subdivisions with room for holds',
    Hard: 'Short bursts, lighter phrases and regular breaks, with a 0.28 s minimum tap interval',
  };
  delete meta.hardSections;
  meta.charts = Object.fromEntries(Object.entries(charts).map(([tier, value]) =>
    [tier, { notes: value.notes.length, holds: value.notes.filter(n => n.holdBeats > 0).length }]));
  write(file, meta);
  console.log(meta.title + ': ' + JSON.stringify(meta.alignment) + ' ' + JSON.stringify(meta.charts));
}
