# inNKX - archival (wcx) plugin for Total Commander

- Posted by: Unavowed
- Version: 1.21 (August 2017)
- Bit: 32bit, 64bit
- Language: Russian only
- System requirements: Windows XP +, Total commander 7.50+ (with full Unicode support)

Description:

- NKS & NKX Archive Unpacker project in the Total Commander shell.
- The plugin allows you to work with NI Kontakt containers (`*.nks, *.nkx, *.nkr, *.nicnt`) as with archives [#1](#note-1).
- Supported:
  - view the contents of containers;
  - extract, delete, add new files (existing containers);
  - creation of new containers;
  - testing (checking the file structure and file extraction capabilities).
  
## Types of containers

The contents of the containers NI Kontakt is divided into 3 groups:

1. files with extensions `*.wav, *.aif (*.aiff), *.ncw` - samples;
2. files with the extensions `*.nki, *.nkm, *.nkb, *.nkg, *.nkp` - patches;
3. files with the extensions `*.nka, *.png, *.tga, *.txt, *.xml, *.db, *.cache, *.meta` - service file types.

Each type of container is designed to store certain types of files.

## Base containers (nks, nkx)

Intended mainly for storing audio material (samples). Kontakt does not impose any restrictions on the content and structure of the base containers:
may contain subdirectories and files of all three types (samples, patches, service files).
Unlike nks-containers, the file table of nkx-containers is written in Unicode-format and file names are not limited to 128 characters.
Samples and addresses of their placement in the containers of proprietary libraries are always encrypted, patches and service files, as a rule, are not encrypted.

## Resource Containers (nkr)

They are intended mainly for storing service files, although their structure is no different from basic (nkx) containers. Resource container files are sometimes encrypted.

Kontakt searches for service files and reverberation pulses only in certain directories of an nkr-container with fixed names:

```
[Resources]
    |__ [ir_samples] (directory for reverb pulses: wav, aif / aiff, ncw)
    |__ [pictures] (Image directory: png, tga) [#2](#note-2)
    |__ [scripts] (script directory: txt)
    |__ [data] (directory for presets: nka)
```

## Notes

### Note 1

Monolithic patches nki, nkm, nkb, nkg are also supported (open with the `Ctrl + PageDown` key combination).

### Note 2

Each image file must be accompanied by the same txt-file of image settings.

That is, Kontakt searches for reverberation pulses only in `Resources\ir_samples\` (subdirectories are not scanned), images only in `Resources\pictures\` (subdirectories are not scanned), etc.
The inNKX plugin treats resource containers (nkr) as nkx containers with a different extension and reserves the right for the user to determine their contents.

## Monolithic patches (nki, nkm, nkb, nkg)

Containers of this type are created by the user, so the files in them are not encrypted. The monolithic container is designed to safely store the patch and its associated samples and service files from the point of view of the integrity of references to samples, since everything is stored in one file and samples cannot be lost.

Monolithic patches created in the 4th version Kontakt are basic (nkx) containers with a different extension and work with them is no different from working with nkx containers, except that you can enter them as archives only by using the Ctrl key combination + PageDown. Such monoliths contain one patch (for example, nki) in the root directory of the container and files associated with it (can be decomposed into subdirectories). Deleting a patch file from the container (nki, nkm, ...) will lead to its inoperability (Kontakt will not be able to download it), however such a container can be used as a base one (after changing the extension to nkx).

Monolithic patches created in version 5 Kontakt are an independent type of container. Unlike the previous ones, such monoliths cannot contain subdirectories, and the patch name is fixed (tool name: patch.nki, multitool: patch.nkm, etc.)

## Registration containers (nicnt)

Intended to store service files required for registering branded libraries on end-user systems.
Kontakt imposes certain restrictions on the contents of the nicnt-containers, which should have the following file structure:

- `[Resources]` directory with required resource files for registration
- `[Library name].xml` main xml file with library registration data
- `ContentVersion.txt` text file with version of library content

The presence of other files in the container root directory, except for these 2 files and the `[Resources]` directory is not allowed. It is also not allowed to delete the above files (although they can be replaced and edited) and the presence of subdirectories in the `[Resources]` directory.
All these features of nicnt-containers are controlled by the plugin and all incorrect operations performed with the container will be stopped or skipped with the user being notified of errors.

- [Library name] is the conditional name of the library, for each library it is its own, unique and must match the parameter ProductHints\Product\Name in the text of the `[Library name].xml` itself.
- This nuance is also controlled by the plugin: when adding a correct xml file with library registration data to the container root directory, the file will be renamed in the container (if the file name on the disk does not match the ProductHints\Product\Name parameter).

In the `ContentVersion.txt` file, only the line with the version of the content (set of samples) of the library in the standard format should be written:
major.minor.[build]

eg:
`1.0.0`
or
`2.1`

In any of the following ANSI, UTF-8, UTF-16 LE encodings.
Any supported files (samples, patches, service files) without subdirectories can be added to the [Resources] directory, but Kontakt will only read files with fixed names from this directory:

- `.LibBrowser.png` image bookmark library in the browser Kontakt
- `.db.cache` is the preinstalled file to import the contents of the library into the Kontakt database.

You can find out the fixed names of other [Resources] catalog files and examine their contents using the example of recently issued nicnt containers of proprietary libraries.
Thus, to create a new nicnt-container, you must first prepare:

1. the main xml-file with the correct registration data of the new library with approximately the following (minimal) content (the ** highlights the parameters that must be unique, do not coincide with other libraries):
   
```xml
<?xml version="1.0" encoding="UTF-8" standalone="no" ?>
<ProductHints spec="1.0.16">
  <Product version="1">
    <Name>**NewLibrary**</Name>
    <Type>Content</Type>
    <Relevance>
      <Application nativeContent="true">kontakt</Application>
    </Relevance>
    <PoweredBy>Kontakt</PoweredBy>
    <Visibility>1</Visibility>
    <Company>My Company</Company>
    <SNPID>**9BF**</SNPID>
    <RegKey>**NewLibrary**</RegKey>
    <ProductSpecific>
      <Visibility type="Number">3</Visibility>
    </ProductSpecific>
  </Product>
</ProductHints>

```

1. text file `ContentVersion.txt` (optional, will be added to the container automatically with the default content version)

2. The `[Resources]` directory (optional, an empty `[Resources]` directory will be automatically added to the container)
Select the prepared files on the Total commander panel, call the packaging dialog (`Alt + F5`), select the “nicnt” item in the dropdown menu “Archiver” and pack the files into the `NewLibrary.nicnt` archive.
After creating the container, you can open it, edit the `ContentVersion.txt`, `NewLibrary.xml` files, add and remove files from the `[Resources]` directory, etc.

The plugin guarantees the correct file structure of the nicnt-container, but its performance is no less determined by the correctness of the registration xml-data.

## Adding files to containers

The plugin allows you to add all 3 groups of file types (samples, patches and service files).
Files with other extensions are skipped (not packaged) with the notification of the user about the missing files.
If necessary, you can add other file types to the group of service files in the plugin settings dialog (available through the Total Commander file packaging dialog).
Encryption of files added to containers is not supported due to the absence of any need for this.

# Extract files from encrypted containers

inNKX can decrypt and extract files from encrypted containers without question. The search for the decryption key is performed automatically in the following order:

1. Local Storage (built-in) key database.
2. Custom key database: the `nklibs_info.userdb` file in the plugin directory [#3](#note-3) (more about it later).
3. Registry (`HKEY_LOCAL_MACHINE\SOFTWARE\Native Instruments\[Library name]\`).
4. Service Center catalog (`%Program Files%\Common Files\Native Instruments\Service Center\[Library name].xml`).
5. Library catalog (`[Library name].nicnt` or `[Library name]_info.nkx`). It is assumed that the target container is in the library (usually in the Samples directory), and the `nicnt` or `_info.nkx` file is in the same directory as the container or one or more levels higher.

In this way, the following container files will be decrypted and extracted:

- library available in the plug-in embedded database [#4](#note-4);
- a registered library (successfully added to the bookmarks of the NI Kontakt browser via the Add Library);
- a library that can be registered (equipped with a working `.nicnt` or `_info.nkx` registration file).
All keys found in the registry, the Service Center catalog and library catalogs (paragraphs 3,4,5), but not in the plugin’s built-in database are stored in the `nklibs_info.userdb` [#5](#note-5) user database.

## Notes

### Note 3

If the `nklibs_info.userdb` file in the plug-in directory is not writable, then the user key database is transferred to the Total Commander root directory: `%COMMANDER_PATH%\nklibs_info.userdb`.

### Note 4

The plugin’s built-in database contains 316 entries, for a complete list, see the end of this document.

### Note 5

`nklibs_info.userdb` is a regular ini-file, each section of which contains registration data of one of the libraries.

## Section Name

SNPID - library identifier.

### Section parameters

- RegKey is a unique library name (more precisely, the name of the registry subkey `HKEY_LOCAL_MACHINE\SOFTWARE\Native Instruments\<RegKey>\` for storing the key);
- `JDX` - key;
- `HU` - initialization vector.

SNPID, RegKey, JDX, HU can be found in the nicnt or _info.nkx file and make an entry about the library in the user database by direct editing nklibs_info.userdb (in a text editor).

**Attention!** Errors entering SNPID, JDX and HU will lead to incorrect decryption of container files, so direct editing is not recommended: when extracting files from a container, registration information will be read and saved in the user database automatically if the nicnt or _info.nkx file is in the same container catalog or one or more levels above.

An example of the section of the ini-file nklibs_info.userdb:
```
[324]
RegKey = Kontakt Factory Library
JDX = E338FA9D6B8D760002CAF80B683FE5A5BD1CF9A644292E3166B8DF44FAD92D8D
HU = C3CCA2803ABC14A68EAACC38EAA7E8EC
```

# UNIX file names

File and directory names in containers created on systems running Mac OS may contain forbidden characters for naming Windows files (`? * " | : < >`), Therefore files with such names cannot be extracted. InNKX solves this problem using escape sequences prohibited characters.
Each forbidden character is assigned a control sequence of characters:

- `\ [bslash]`  backslash
- `? [qmark]`   question mark
- `* [star]`    multiplication sign
- `" [quote]`    double quote
- `| [pipe]`    pipe
- `: [colon]`   colon
- `< [less]`     less sign
- `> [greater]` greater sign
- `_ [space]`   space (only at the end of the name)
- `. [dot]`     dot (only at the end of the name)

When reading files and directories of a container in Total commander, names are transferred in which all forbidden characters are replaced with their control sequences [#6](#note-6). When extracting to disk, the user will be warned that the file names on the disk and in the container are different. When packing a file or directory whose name contains control sequences, the reverse transformation will be performed.
Example [#7](#note-7):
```
    file name in the container; .PAResources|database|PAL|PAL.meta
    operation:                  unpacking [down arrow] [up arrow] packaging
    file name on disk           .PAResources[pipe]database[pipe]PAL[pipe]PAL.meta
```

There is some possibility that the file name (directory) of the container will contain the control sequences themselves. In order to block the conversion of the control sequence to the corresponding forbidden character during repacking, all opening angle brackets before the control sequence will be doubled, and the user will be warned that the names of the files on the disk and in the container are different.
Example:
```
    file name in                [more][music][less][noise].aif container
    operation:                  unpacking [down arrow] [up arrow] packaging
    disk file name              [more][music][[less][noise].aif
```

Thus, an even number of opening angle brackets in front of the control sequence shields it (blocks conversion to a forbidden character when packed), but each pair of these brackets (`[[`) will be replaced with a single (`[`).

Example:
```
    file name on disk                     package name in container
    [[[[[pipe]]organ[[[colon]]A#.aiff   → [[[pipe]]organ[:]A#.aiff
```

## Notes

### Note 6 

It should be clear that Total commander will always display the “corrected” file name (the file name on the disk in the examples), regardless of where it is on the disk or in the container.

### Note 7

As an example, the name of one of the files in the container `Una Corda.nicnt` (`Native Instruments GmbH Una Corda`) is taken.

**Hidden directories:**
The directories on the disk with the attribute “hidden” after being packed into the container will be hidden for the Kontakt browser, and vice versa, the directories in the container that are hidden for the Kontakt browser will have the attribute “hidden” after unpacking to disk. In order to hide the entire contents of the newly created container for the Kontakt browser (hide the container root directory), the packaged files and directories must be located in the directory with the “hidden” attribute.


# Version history

## 1.20
Added:
- Support for encrypted containers of the Kontakt 5.6.8 format

Keys for decrypting library containers:
- Best Service The Orchestra
- Big Fish Audio Sequence
- Big Fish Audio Vintage Horns 2
- Blinksonic AETONZ
- Blinksonic RUIDOZ
- Blinksonic SUBSTANZ
- Blinksonic VOZ
- Bluescreen Productions Berlin Blue Bass
- Embertone The Joshua Bell Violin
- In Session Audio Fluid Harmonics
- In Session Audio Fluid Strike
- Heavyocity BitRate II & MonoBoy
- Heavyocity Calc-U-Synth
- Heavyocity C-Tools
- Heavyocity GP04 Vocalise 2
- Heavyocity GRID II
- Luftrum Lunaris
- Native Instruments GmbH Middle East
- Native Instruments GmbH Symphony Essentials Collection
- Native Instruments GmbH Symphony Essentials Percussion
- Native Instruments GmbH Symphony Series Collection
- Native Instruments GmbH Symphony Series Percussion
- Orchestral Tools Berlin Orchestra Inspire
- Sample Logic Electro City
- Sample Magic Klip
- Secret Room Music STRIKEFORCE
- Soniccouture The Canterbury Suitcase
- Soundiron Ambius
- Sound Yeti Collision FX
- Spitfire Audio Spitfire Symphonic Strings Evolutions
- Tonsturm WHOOSH
- Tim Exile SLOO
- Tim Exile SLOR
- Versilian Studios Chamber Orchestra 2.6 Professional Edition

## 1.18

Fixed:
- After performing file operations with the nicnt container, the plugin automatically assigns it the wrong double extension (.xml.nicnt)

Added:
- Support for unencrypted containers of the new format (Kontakt 5.6.8)

Keys for decrypting library containers:
- ProjectSAM Swing More!
- Heavyocity NOVO
- Tovusound Edward Foleyart Instrument
- Sonokinetic Espressivo
- In Session Audio Riff Generation
- Cinesamples CineBrass Twelve Horn Ensemble
- Tru-Urban Kevin Keys by Kevin Randolph
- Native Instruments GmbH Thrill
- Soniccouture Electro Acoustic
- Tonehammer Solo Frame Drums
- Tonehammer Bowed Grand
- Tonehammer Epic Frame Drum Ensemble
- Spitfire Audio Spitfire British Wood
- Spitfire Audio Bernard Herrmann Composer Toolkit

## 1.17a

Added:
Keys for decrypting library containers:
- Output ANALOG STRINGS
- Output ANALOG STRINGS
- Impact Soundworks Straight Ahead Jazz Horns
- Sample Logic IMPAKT
- Sample Modeling The Trumpet 3
- Sample Modeling French Horn and Tuba 3
- Heavyocity GP03 Scoring Guitars
- Sample Logic Rhythmology
- DAC Acoustic Services Indigisounds
- Rattly and Raw Martin France Drums
- C-Dub Whoop Triggerz Ultimate
- Drumasonic drumasonic Xplosive
- Realitone Hip Hop Creator
- Wave Alchemy Revolution
- Umlaut Audio uBEAT Hip Hop
- Spitfire Audio Spitfire Symphonic Woodwinds
- Soniccouture Ondioline
- Tomahawk Sounds Big Hands Vol 1
- Kirk Hunter Studios Virtuoso Ensembles
- Fable Sounds Broadway Gig
- Embertone Intimate String Chords
- Best Service Chris Hein Orchestral Brass
- Spitfire Audio Spitfire Symphonic Brass
- JGR Production The Performer - The Voice of the Chrysler 300C
- Heavyocity Master Sessions Ensemble Drums
- Spitfire Audio The Grange
- The Loop Loft Drum Direktor - From The Garage
- Spitfire Audio Spitfire Chamber Strings
- Heavyocity Master Sessions Ensemble Metals
- Umlaut Audio uBEAT Elektro
- Tovusound Edward Ultimate Suite
- Heavyocity Master Sessions Ensemble Woods
- Spitfire Audio Albion V
- Spitfire Audio Spitfire Symphonic Strings
- Room Sound Kurt Ballou Signature Series Drums
- Native Instruments GmbH Reaktor Factory Selection R2
- PREMIER Engineering Drum Tree
- Audio Impressions 70 DVZ Strings
- Spitfire Audio Albion IV
- Best Service Chris Hein Orchestral Brass Compact
- Spitfire Audio Fanshawe Vol 1
- Soniccouture The Hammersmith
- Native Instruments GmbH Kinetic Toys
- Divergent Audio Group Invasors
- Soundiron Voices of Rapture
- DAC Acoustic Services Laventille Rhythm Section
- Umlaut Audio uBEAT Hybrid
- Native Instruments GmbH Symphony Essentials Brass Collection
- Ethnaudio Strings Of Anatolia
- Spitfire Audio Spitfire Masse
- DAC Acoustic Services Soca Starter Pack - Volume 1
- Best Service Chris Hein Winds Compact
- Volkswagen AG Volkswagen Brand Instruments
- e-Instruments Session Keys Electric S
- Spitfire Audio Spitfire North 7 Vintage Keys
- Native Instruments GmbH Symphony Essentials Woodwind Collection
- Drumdrops Drumdrops Folk Rock Kits
- Spitfire Audio London Contemporary Orchestra Strings
- Get Good Drums Matt Halpern Signature pack
- Big Fish Audio Vital Series Mallets

## 1.17

Fixed:
- The plugin extracts files from containers of only those libraries whose identifiers begin with a significant digit.
- The plugin crashes when extracting files whose names end with a period or a space.

Added:
- Support for Windows XP.
- Key database browser for decrypting containers with the ability to export and import records (implemented as a file system plugin and is accordingly available in the Total - commander network environment).
- Decryption of containers of protected libraries without extracting files (launch in the plugin settings dialog: Alt + F5 -> nkx -> Settings ...)

Keys for decrypting library containers:
- Bechstein Digital Grand
- Best Service Chris Hein - Solo Cello
- Best Service Chris Hein - Solo ContraBass
- Best Service Chris Hein - Solo Viola
- Best Service Chris Hein Solo Violin
- Best Service Ethno World 6 Instruments
- Best Service Ethno World 6 Voices
- Best Service Kwaya
- Big Fish Audio Cyborg
- Big Fish Audio Grindhouse
- Cinematic Samples Cinematic Studio Piano
- Cinematic Samples Cinematic Studio Strings
- Cinematique Instruments Fabrique
- Cinesamples Abbey Road Classic Upright Pianos
- Cinesamples CineBrass Descant Horn
- Cinesamples CinePerc
- Cinesamples CineStrings Solo
- Cinesamples Cinesymphony LITE
- Cinesamples CineWinds CORE
- Cinesamples CineWinds PRO
- Cinesamples Rio Grooves
- Embertone Fischer Viola
- Embertone Leonid Bass
- Ethnaudio Breath Of Anatolia
- Evolution Series The World Percussion 2.0
- Heavyocity Gravity
- Heavyocity GP01 Natural Forces
- Heavyocity GP02 Vocalise
- Heavyocity Master Sessions Ethnic Drum Ensembles
- Impakt Soundworks Shreddage Drums
- Impakt Soundworks Super Audio Cart
- Native Instruments GmbH Discovery Series: India
- Native Instruments GmbH Kinetic Treats
- Native Instruments GmbH Session Guitarist - Strummed Acoustic 2
- Native Instruments GmbH Scarbee Classic EP-88s
- Native Instruments GmbH Symphony Essentials Brass Ensemble
- Native Instruments GmbH Symphony Essentials Brass Solo
- Native Instruments GmbH Symphony Essentials String Ensemble
- Native Instruments GmbH Symphony Essentials Woodwind Ensemble
- Native Instruments GmbH Symphony Essentials Woodwind Solo
- Native Instruments GmbH Symphony Series Woodwind Ensemble
- Native Instruments GmbH Symphony Series Woodwind Solo
- Orange Tree Samples Evolution Dracus
- Orange Tree Samples Evolution Flatpick 6
- Orange Tree Samples Evolution Jazz Archtop
- Orange Tree Samples Evolution Mandolin
- Orange Tree Samples Evolution Modern Nylon
- Orange Tree Samples Evolution Sitardelic
- Orange Tree Samples Evolution Steel Strings
- Orange Tree Samples Evolution Stratosphere
- Orange Tree Samples Evolution Strawberry
- Orange Tree Samples Evolution Rick 12
- Orchestral Tools Berlin Brass
- Orchestral Tools Berlin Percussion
- Orchestral Tools Metropolis Ark 1
- Orchestral Tools Metropolis Ark 2
- Output SUBSTANCE
- ProjectSAM SWING!
- Q Up Arts California Keys
- Realitone Fingerpick
- Realitone RealiDrums
- Refractor Audio Transport
- Sample Logic Bohemian
- Sample Logic Cinematic Guitars Organic Atmospheres
- Sample Logic CinemorphX
- Sample Logic Cinematic Guitars Infinity
- Sample Logic Cyclone
- Sample Logic Gamelan
- Synthepica Epica Bass
- Soniccouture The Attic
- Soniccouture Balinese Gamelan 2
- Soniccouture Estey Reed Organ
- Soniccouture Xbow Guitars
- Sonicsmiths The Foundry
- Sonokinetic Capriccio 
- Sonokinetic Maximo
- Sonokinetic Orchestral Series - Woodwinds Ensemble
- Sonokinetic Ostinato
- Sonokinetic Sotto
- Sonokinetic Tutti
- Sonokinetic Tutti Vox
- Soundiron Mercury Elements Boys Choir
- Soundiron Elysium Harp
- Spitfire Audio Albion ONE
- Spitfire Audio Hans Zimmer Piano
- The Loop Loft Drum Direktor Cinematik
- The Loop Loft Drum Direktor FNK-4
- Umlaut Audio ARPS
- Umlaut Audio PADS
- Umlaut Audio uBEAT Bundle
- Vir2 Aeris Hybrid Choir Designer
- vi elements Core Kit
- Wavesfactory Wavesfactory - Mercury
- Wide Blue Sound Eclipse
- Wide Blue Sound Orbit

## 1.10

Expansion of functionality:
- Added support for monolithic patches (nki, nkm, nkb, nkg), created in NI Kontakt 5th version;
- Added support for nicnt-containers.

Added plugin settings dialog (available via file packaging dialog - standard Alt + F5 shortcut).

Added keys to decrypt library containers:
- Sonic Faction Archetype
- Native Instruments GmbH Una Corda
- Native Instruments GmbH Symphony Series Brass Ensemble
- Native Instruments GmbH Symphony Series String Ensemble
- Impact Soundworks Shreddage Bass 2

## 1.05

Fixed errors in extracting library registration data from *.nicnt and * _info.nkx containers:
- the plugin does not find *.nicnt and * _info.nkx files in the root directory of the logical drive;
- the plugin does not find the registration data available in the info-container.

Added keys to decrypt library containers:
- Big Fish Audio Vintage Rhythm Section
- Big Fish Audio Ambient Black
- Tonehammer Plucked Grand Piano
- Output EXHALE
- Soniccouture Box of Tricks
- Native Instruments GmbH Symphony Series Brass Solo

## 1.04

Fixed a bug causing the plug-in to freeze: when opening fake containers smaller than 4 bytes, the reading progress window does not close.

Added keys to decrypt containers:
- Prominy Hummingbird

## 1.03

First release.


# Plugin's built-in database (complete list of keys to decrypt files):
## SNPID (key identifier) ​​RegKey (unique library name)
13. Keyboard Collection
14. [not known]
101. Stradivari Solo Violin
102. [not known]
103. otto
104. Acoustic Legends HD
105. Ambience Impacts Rhythms
106. Chris Hein - Guitars
107. Solo Strings Advanced
108. [not known]
110. [not known]
111. Drums Overkill
112. [not known]
113. [not known]
114. [not known]
115. [not known]
116. Gofriller Cello
117. [not known]
118. [not known]
119. [not known]
120. [not known]
152. The Giant
156. DrumMic'a!
157. juggernaut
158. Drum Lab
164. HAVOC
166. Geosonics
167. Friedlander Violin
168. Clav
169. Grand Marimba
193. Vintage Horns
194. SWAGG
195. lumina
196. AEON Melodic
197. AEON Rhythmic
198. Complete Orchestral Collection
199. drumasonic LUXURY
214. Olympus Elements
215. MegaMacho Drums
217. Virtual Ensemble Triolgy
218. EPIC
219. Blakus Cello
221. Cinematic Thunder
222. The Trombone 3.0
224. Shevannai
225. Action Strikes
226. Minimal
227. Zodiac
228. Vintage VI
229. Kinetic Metal
231. Apocalypse Elements
232. Berlin Strings
242. Vibraphone
245. CineStrings CORE
246. Epica
293. DM307
296. Altus
297. Realivox Blue
313. HZ Percussion
314. Session Horns Pro
324. Kontakt Factory Library
325. Rise And Hit
326. Cantus
327. Session Keys Grand S
328. Session Keys Grand Y
388. Symphobia Colors Animator
389. Symphobia Colors Orchestrator
390. Acou6tics
392. Archtop
396. Chris Hein - Winds Vol 3
397. Chris Hein - Winds Vol 1
398. Chris Hein - Winds Vol 2
399. Chris Hein - Winds Vol 4
401. Kontakt Factory Selection
402. Maschine Drum Selection
405. Evolve Mutations
406. Scarbee Pre-Bass
407. Scarbee Pre-Bass Amped
408. Balinese Gamelan
409. Upright Piano
410. Berlin Concert Grand
411. New York Concert Grand
412. Vienna Concert Grand
413. Scarbee MM-Bass
414. Scarbee MM-Bass Amped
415. Scarbee Jay-Bass
416. Abbey Road 60s Drums
417. Alicias Keys
418. Scarbee Clavinet Pianet
419. Evolve Mutations 2
420. Scarbee A-200
421. Scarbee Mark I
422. Scarbee Vintage Keys
423. Abbey Road 70s Drums
424. Session Strings
425. Vintage Organs
426. Kontakt Elements Selection
427. Abbey Road 60s Drums Vintage
428. Abbey Road 80s Drums
429. Abbey Road Modern Drums
430. George Duke Soul Treasures
431. Scarbee Funk Guitarist
432. Kontakt Elements Selection R2
433. West Africa
434. Session Strings Pro
435. Studio Drummer
436. Retro Machines Mk2
437. Damage
438. Abbey Road 60s Drummer
439. Abbey Road 70s Drummer
440. Abbey Road 80s Drummer
441. Abbey Road Modern Drummer
442. Evolve R2
454. Action Strings
455. Session Horns
457. Abbey Road Vintage Drummer
459. Scarbee Rickenbacker Bass
469. Abbey Road 50s Drummer
472. Digital Revolution
473. Urban Legacy XXL
474. GOTH
475. ARPOLOGY
481. CineStrings - RUNS
482. Artist Series - Tina Guo
483. Grosso
484. Hummingbird
486. The Maverick
487. The Grandeur
488. The Gentleman
494. HZ02 Percussion
496. B-11X Multi-timbral Synth and Organ
497. Dirty Modular
519. Earth
540. Cuba
541. Cinematic Keys
542. LADD
545. calliope
547. Mystica
548. Smack
549. Vintage Strings
551. Xosphere
552. HZ03
553. Orchestral Essentials 2
555. arcane
557. Apollo Cinematic Guitars
558. Signal
559. Hammersmith Pro
560. fuse
568. Session Guitarist - Strummed Acoustic
569. Emotive Strings
571. Ambient White
574. Emotional Cello
578. Session Keys Electric R
585. Box of Tricks
588. PEARL Concert Grand
590. Symphony Series Brass Solo
591. Symphony Series Brass Ensemble
595. Ambient Black
619. Bravura Scoring Brass
633. Symphony Series String Ensemble
639. REV X-LOOPS
640. EXHALE
645. Vintage Rhythm Section
659. Una Corda
684. Evolution Banshee
689. Archetype
695. Shreddage Bass 2
709. Neo-Soul Keys
744. Fractured
747. Cinebrass
748. CineBrass PRO
781. Chris Hein Horns Pro Complete
794. VI.ONE
796. Jazz and Big Band KP2
797. London Solo Strings KP2
798. First Call Horns KP2
799. Garritan Personal Orchestra KP2
800. syntAX
801. Virtual Drumline 2.5
802. galaxy ii
803. Concert and Marching Band
804. Symphonic Sphere
805. Orchestral String Runs
806. Pop Rock Strings
807. [not known]
809. RIG
810. VOXOS
811. Q
812. Hollywoodwinds
813. Cinematic Guitars
814. Violence
815. SR5 Rock Bass
816. [not known]
817. [not known]
818. Circus Circuit Bending
819. Chris Hein Horns Vol 3
820. Chris Hein Horns Vol 4
821. CHH Compact
822. LA Scoring Strings
823. Galaxy Vintage D
824. accordions
825. Ethno World 5 Voices and Choirs
826. Ethno World 5 Instruments
827. Mr. Sax t
828. The Trumpet
829. Prominy SC
830. Mixosaurus Kit A
831. Presonus Virtual Instrument
832. The Elements
833. Phaedra
834. Complete Classical Collection KP2
835. String Essentials KP2
836. Ethno World 4
837. Chris Hein Bass
838. Convolution Space
839. Galaxy Steinway OEM
840. Xsample Chamber Ensemble
841. Elite Orchestral Percussion
842. BASiS
843. Electri6ity
844. mojo
845. World Impact
846. Virtual Grand Piano 2
847. Ocean Way Drums Gold
848. Ocean Way Drums Platinum
849. Evolve
850. Chris Hein Horns Solo
851. Chris Hein Horns Vol 2
852. Synergy
853. Kreate
854. Symphobia
855. Orchestral Brass Classic
856. Virtual Bouzouki
857. Chamber Strings
858. Symphonic Strings
859. Great British Brass
860. Studio
861. Plectrum
862. Ocean Way Expandable
863. Steven Slate Drums LE
864. Steven Slate Drums Platinum
865. Steven Slate Drums EX
867. Mr. Sax B + A
868. The Sax Brothers
869. The Trombone
874. Broadway Big Band
875. Broadway Lites
878. Cyclone
879. Assault
880. Morphestra
881. Rhythm Objekt
882. Glassworks
883. Drumasonic
884. Tubes!
885. True Strike
886. True Strike 2
887. Symphobia 2
888. Concert Harp
889. Organ Mystique
890. LA Scoring Strings Lite
891. LA Scoring Strings First Chair
893. LASS Legato Sordino
895. French Horn and Tuba
897. Iceni
899. Spitfire Percussion
900. Chris Hein Bass DE
901. Ethno World 4 Pro DE
902. Galaxy II DE
903. Chris Hein - Guitars DE
904. Xsample Chamber Ensemble DE
905. Chris Hein Horns Solo DE
906. Chris Hein Horns Vol 2 DE
908. Galaxy Vienna Grand
909. Galaxy Steinway
910. Galaxy German Baby Grand
911. Studio Kit Builder
912. rumble
913. Fanfare
914. Vintage Vibe
917. Emotional Piano
918. Plucked Grand
919. Requiem Light
921. EP73 Deconstructed
922. Konkrete
923. Broken Wurli
924. Novachord
925. Array Mbira
926. Xtended Piano
927. Ondes Martenot
928. Pan Drums 2
929. The Conservatoire Collection
931. Nitron
932. Sasha Soundlab
933. Realivox The Ladies
935. Albion
936. V-Metal
937. Orchestral Essentials
938. MKSensation
978. Cinematic Strings
979. Vivace
980. Transistor Revolution
982. Black
983. The 13 Cosmic Rays
984. Tonys Old School Bass
985. Tonys Bright and Funky Bass
986. Tonys Double Neck Bass
987. Art Vista Back Beat Bass
989. Solo Violino Classico
990. Solo Violino Virtuoso
991. Berlin Woodwinds
992. Loegria
993. Cinematic Guitars 2
995. ZapZorn Elements
996. Shreddage II
4999. [not known]
5000. UserPatches

