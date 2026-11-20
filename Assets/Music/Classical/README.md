# Classical song collection

The game offers exactly five real songs:

| Composer | Song | Playable audio |
| --- | --- | --- |
| Beethoven | Für Elise | 2:03 |
| Pachelbel | Canon in D | 2:00 |
| Bach | Prelude in C | 2:22 |
| Mozart | Rondo K.545 (Sonata K.545, third movement) | 1:47 |
| Chopin | Prelude in E minor, Op.28 No.4 | 2:10 |

Every session stays below 2:30 including READY, its fade, the initial scheduling buffer and the four-second chart/audio lead-in. The average music length is about 2:05.

Each song has its own folder containing its playable MP3, downloaded cover image, source MIDI, Credits.txt, ChartingNotes.json, and a Charts folder. The two shortened recordings also retain the unmodified downloads at Sources/Original.mp3, with the edit recorded in AudioEdit.json. Their last chart notes and hold tails end before the final fade-out.

Recording and MIDI attribution: [A-M Classical](https://amclassical.com/) under [CC BY 3.0 US](https://creativecommons.org/licenses/by/3.0/us/). Public-domain cover sources and exact download URLs are documented in each Credits.txt. The game displays amclassical.com with the selected song.

## Song catalogue and charts

The catalogue is Assets/Data/ClassicalSongs.asset. It contains five entries, each with its cover, audio, preview region and Therapy/Easy/Medium/Hard chart references. There are no placeholder entries or cards in the browser.

The 20 SongChart assets are editable in the Inspector. Therapy uses the fixed height selected by the player. Easy follows spaced musical accents, Medium adds subdivisions, and Hard uses short runs and lighter phrases. Hard notes are at least 0.28 seconds apart. After three close gaps, the chart waits at least 0.76 seconds before the next note. Height changes are smaller during fast runs. Simultaneous chord notes share one target height, and holds use sustained notes with recovery time before the next tap.

Chart timestamps follow each source MIDI tempo map, with local adjustments from attacks in the recorded audio. A note's beat value is its absolute timestamp multiplied by the song's reference BPM / 60. This preserves tempo changes. All saved times already include the four-second audio lead-in. READY runs before the chart clock starts. Alignment estimates and counts are in ChartingNotes.json.

To rebuild, run `Tools/analyse-classical-midi.cjs` and `AnalyseClassicalAudio.Main` in Unity, then run `Tools/chart-classical-songs.cjs`. Use `AuthorClassicalSongs.UpdateCharts` to write the resulting JSON into the 20 existing Unity chart assets. This step overwrites Inspector chart edits from JSON.

## Audio controls and previews

Click a card to hear a looping 20-second excerpt. Choosing another card switches the song. Preview playback pauses menu background music and stops on Continue/Home or when leaving the scene.

Music volume controls songs, previews and menu music. Beat / hit volume controls note-hit sounds and practice/debug metronomes. UI sound volume controls button clicks. Music and note-hit audio use separate authored sources.
