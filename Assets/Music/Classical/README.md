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

The 20 SongChart assets are editable in the Inspector. Therapy uses the fixed height selected by the player. Free Mode Easy contains the original Medium chart, and Medium contains the original Hard chart. Hard alternates fast MIDI streams with lighter phrases and approximately 1–2 second no-input rests. Fast taps can be as close as 0.12 seconds apart; lighter sections use varied 0.45–0.8 second minimum spacing. Simultaneous chord notes share one target height. Holds end before rests and leave at least 0.2 seconds before the next tap. Phrase boundaries and rest regions are recorded in ChartingNotes.json. All modes include taps and holds.

Chart timestamps follow each source MIDI tempo map, aligned against the corresponding MP3 waveform. A note's beat value is its absolute timestamp multiplied by the song's reference BPM / 60; this preserves tempo changes. All saved times already include the four-second audio lead-in. READY runs before the chart clock starts. Alignment estimates and counts are in ChartingNotes.json.

To rebuild, use Tools/analyse-classical-midi.cjs and Tools/AnalyseClassicalAudio.cs, then Tools/chart-classical-songs.cjs, which also applies Tools/rebalance-free-charts.cjs. The rebalancer can also run independently using the stored MP3 alignment; its version marker prevents repeatedly shifting difficulties on subsequent runs. Bind the resulting JSON to Unity chart assets and refresh the five song cards with Tools/AuthorClassicalSongs.cs. Re-running that authoring tool overwrites Inspector chart edits from JSON.

## Audio controls and previews

Click a card to hear a looping 20-second excerpt. Choosing another card switches the song. Preview playback pauses menu background music and stops on Continue/Home or when leaving the scene.

Music volume controls songs, previews and menu music. Beat / hit volume controls note-hit sounds and practice/debug metronomes. UI sound volume controls button clicks. Music and note-hit audio use separate authored sources.
