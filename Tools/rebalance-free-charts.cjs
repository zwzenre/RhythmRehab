const fs = require('fs');
const folders = ['Beethoven_FurElise', 'Pachelbel_CanonInD', 'Bach_PreludeInC', 'Mozart_SonataK545_Rondo', 'Chopin_PreludeInEMinor'];
const round = value => Math.round(value * 1e6) / 1e6;
const read = file => JSON.parse(fs.readFileSync(file, 'utf8'));
const write = (file, value) => fs.writeFileSync(file, JSON.stringify(value, null, 2) + '\n');

for (const [songIndex, folder] of folders.entries()) {
  const dir = `Assets/Music/Classical/${folder}`;
  const meta = read(`${dir}/ChartingNotes.json`);
  const midi = read(`Temp/ClassicalAnalysis/${folder}.json`);
  const charts = {};
  for (const name of ['Therapy', 'Easy', 'Medium', 'Hard']) charts[name] = read(`${dir}/Charts/${name}.json`);

  // Versioning makes a second run safe: only shift the legacy charts once.
  if (meta.freeDifficultyVersion !== 2) {
    charts.Easy = { ...charts.Medium, description: charts.Medium.description.replace('— Medium.', '— Easy.') + ' Promoted from the original Medium chart.' };
    charts.Medium = { ...charts.Hard, description: charts.Hard.description.replace('— Hard.', '— Medium.') + ' Promoted from the original Hard chart.' };
  }

  // Group chord notes into a single attack; use the highest pitch for melody height.
  const attacks = [];
  for (const n of midi.notes) {
    let attack = attacks.at(-1);
    if (!attack || n.time - attack.time > .018) {
      attack = { time: n.time, beat: n.beat, pitch: n.pitch, velocity: n.velocity, duration: n.end - n.time };
      attacks.push(attack);
    } else {
      attack.pitch = Math.max(attack.pitch, n.pitch);
      attack.velocity = Math.max(attack.velocity, n.velocity);
      attack.duration = Math.max(attack.duration, n.end - n.time);
    }
  }
  const pitches = attacks.map(n => n.pitch).sort((a, b) => a - b);
  const low = pitches[Math.floor(pitches.length * .1)], high = pitches[Math.floor(pitches.length * .9)];
  const edit = fs.existsSync(`${dir}/AudioEdit.json`) ? read(`${dir}/AudioEdit.json`) : null;
  const end = Math.min(meta.audioDuration - 1.5, edit ? edit.sourceEndSeconds - edit.fadeOutSeconds - .9 : meta.audioDuration - 1.4);
  const candidates = attacks.map(n => ({ ...n, time: n.time * meta.alignment.scale + meta.alignment.offsetSeconds }))
    .filter(n => n.time >= 0 && n.time < end);
  // Alternate fast runs, lighter phrases, and genuine no-input breathing spaces.
  // Snap boundaries to nearby musical attacks, favouring beats and quieter gaps.
  const boundary = (start, duration) => {
    const desired = start + duration;
    let best = Math.min(end, desired), bestScore = -Infinity;
    for (let i = 0; i < candidates.length; i++) {
      const n = candidates[i];
      if (n.time < desired - .35 || n.time > desired + .5 || n.time <= start + .8) continue;
      const onBeat = Math.abs(n.beat - Math.round(n.beat)) < .08;
      const gap = i + 1 < candidates.length ? candidates[i + 1].time - n.time : .5;
      const score = (onBeat ? .5 : 0) + Math.min(.5, gap) + (127 - n.velocity) / 300 - Math.abs(n.time - desired);
      if (score > bestScore) { bestScore = score; best = n.time; }
    }
    return Math.min(end, best);
  };
  const sections = [];
  const phrases = [[7, 3, 1.6], [9, 4, 2], [6.5, 3.5, 1.5]];
  let sectionStart = candidates[0].time, cycle = 0;
  while (sectionStart < end - .01) {
    const lengths = phrases[(cycle + songIndex) % phrases.length];
    for (const [i, kind] of ['burst', 'light', 'rest'].entries()) {
      if (sectionStart >= end - .01) break;
      const finish = boundary(sectionStart, lengths[i]);
      sections.push({ kind, start: sectionStart, end: finish });
      sectionStart = finish;
    }
    cycle++;
  }
  const notes = [];
  let cursor = 0, next = 0, height = 0, previousTime = -10, sectionIndex = 0;
  while (cursor < candidates.length) {
    while (cursor < candidates.length && candidates[cursor].time < next - .000001) cursor++;
    if (cursor >= candidates.length) break;
    // Keep consecutive musical attacks, including fast piano runs. Only merge
    // near-simultaneous attacks that a single height marker cannot distinguish.
    const n = candidates[cursor++];
    while (sectionIndex < sections.length - 1 && n.time >= sections[sectionIndex].end - .000001) sectionIndex++;
    const section = sections[sectionIndex];
    if (section.kind === 'rest') { next = section.end; continue; }
    const melodic = Math.max(0, Math.min(1, (n.pitch - low) / Math.max(1, high - low)));
    const phrase = Math.floor(n.beat / 8);
    const contour = [0, .25, .5, .75, 1, .75, .5, .25][(phrase + songIndex * 2) % 8];
    const target = Math.round((melodic * .7 + contour * .3) * 16) / 16;
    // Closely spaced notes use smaller height changes than widely spaced phrases.
    const step = Math.min(.625, (n.time - previousTime) * 1.25);
    height = Math.max(0, Math.min(1, Math.max(height - step, Math.min(height + step, target))));
    const gapToNextAttack = cursor < candidates.length ? candidates[cursor].time - n.time : end - n.time;
    const wantsHold = (notes.length + songIndex * 2) % 48 === 24 || (n.duration >= .9 && (notes.length + songIndex * 2) % 32 === 5) || gapToNextAttack >= 1;
    let hold = wantsHold ? Math.min(.9, Math.max(.45, Math.min(n.duration, gapToNextAttack - .2)), end - n.time - .3, section.end - n.time - .2) : 0;
    if (hold < .4) hold = 0;
    notes.push({ beat: round((n.time + meta.leadIn) * meta.bpm / 60), lane: Math.round(height * 2), useCustomHeight: true, customHeight: round(height), holdBeats: round(hold * meta.bpm / 60) });
    previousTime = n.time;
    const tapGap = section.kind === 'burst' ? .12 : [.45, .65, .8][(Math.floor(sectionIndex / 3) + songIndex) % 3];
    next = n.time + (hold > 0 ? hold + .2 : tapGap);
  }
  if (notes.length <= charts.Medium.notes.length) throw Error(`${folder}: Hard must contain more notes than Medium`);
  charts.Hard = { description: `${meta.title} — Hard. Alternating piano-tile-style bursts, lighter musical phrases and no-input rests. Burst taps follow MIDI attacks as close as 0.12 seconds apart; lighter passages use varied spacing. Chords share one height, and holds finish before rest sections. Four-second audio lead-in.`, durationSeconds: charts.Medium.durationSeconds, notes };
  for (const name of ['Easy', 'Medium', 'Hard']) write(`${dir}/Charts/${name}.json`, charts[name]);
  meta.freeDifficultyVersion = 2;
  meta.freeDifficultyDesign = { Easy: 'Original Medium chart', Medium: 'Original Hard chart', Hard: 'Fast MIDI bursts alternate with lighter phrases and 1–2 second no-input rests; boundaries follow nearby musical attacks' };
  meta.hardSections = sections.map(s => ({ kind: s.kind, startSeconds: round(s.start + meta.leadIn), endSeconds: round(s.end + meta.leadIn) }));
  meta.charts = Object.fromEntries(Object.entries(charts).map(([name, chart]) => [name, { notes: chart.notes.length, holds: chart.notes.filter(n => n.holdBeats > 0).length }]));
  write(`${dir}/ChartingNotes.json`, meta);
  console.log(`${meta.title}: Easy ${charts.Easy.notes.length}, Medium ${charts.Medium.notes.length}, Hard ${notes.length}`);
}

