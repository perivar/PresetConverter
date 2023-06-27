unit WAVParser3;

interface

uses Classes,SysUtils,Windows, MappedFileUnit;

const TEST_GUID:array [0..15] of char = #01+#00+#00+#00+#00+#00+#16+#00+#128+#00+#00+#170+#00+#56+#155+#113;

const MAX_SHORTINT = 128;
      MAX_SMALLINT = 32768;
      MAX_24BIT = 2147483648;
      MAX_BYTE = 255;

type T24Bit = array [0..2] of byte;
     P24Bit = ^T24Bit; 
     TSampleStereo8bit = array [0..1] of byte;
     TSampleStereo16bit = array [0..1] of smallint;
     TSampleStereo24bit = array [0..1] of T24Bit;
     TWAV8bitMono   = array [0..0] of byte;
     TWAV8bitStereo = array [0..0] of TSampleStereo8bit;
     TWAV16bitMono   = array [0..0] of smallint;
     TWAV16bitStereo = array [0..0] of TSampleStereo16bit;
     TWAV24bitMono   = array [0..0] of T24Bit;
     TWAV24bitStereo = array [0..0] of TSampleStereo24bit;
     PWAV8bitMono   = ^TWAV8bitMono;
     PWAV8bitStereo = ^TWAV8bitStereo;
     PWAV16bitMono   = ^TWAV16bitMono;
     PWAV16bitStereo = ^TWAV16bitStereo;
     PWAV24bitMono   = ^TWAV24bitMono;
     PWAV24bitStereo = ^TWAV24bitStereo;


type TFileWAVHeader = packed record
              RIFFtag:array [0..3] of char;
              Filesize:DWORD;
              WAVEtag:array [0..3] of char;
              FMTtag:array [0..3] of char;
              chnksize:DWORD;
              wFormatTag: Word;         { format type }
              nChannels: Word;          { number of channels (i.e. mono, stereo, etc.) }
              nSamplesPerSec: DWORD;  { sample rate }
              nAvgBytesPerSec: DWORD; { for buffer estimation }
              nBlockAlign: Word;      { block size of data }
              wBitsPerSample: Word;   { number of bits per sample of mono data }
            end;

type TFileWAVHeaderEx = packed record
              RIFFtag:array [0..3] of char;
              Filesize:DWORD;
              WAVEtag:array [0..3] of char;
              FMTtag:array [0..3] of char;
              chnksize:DWORD;
              wFormatTag: Word;         { format type }
              nChannels: Word;          { number of channels (i.e. mono, stereo, etc.) }
              nSamplesPerSec: DWORD;  { sample rate }
              nAvgBytesPerSec: DWORD; { for buffer estimation }
              nBlockAlign: Word;      { block size of data }
              wBitsPerSample: Word;   { number of bits per sample of mono data }
              cbSize: Word;           { the count in bytes of the size of }
              realbps: Word;    { valid bits per sample }
              speakers: DWORD;  { speaker/channel mapping }
              GUID: array [0..15] of char;
            end;

type TChunkHeader = packed record
              Tag:array [0..3] of char;
              Size:DWORD;
            end;

type TMyWAVHeader = record
              RIFFtag:array [0..3] of char;
              Filesize:DWORD;
              WAVEtag:array [0..3] of char;
              FMTtag:array [0..3] of char;
              chnksize:DWORD;
              wFormatTag: Word;         { format type }
              nChannels: Word;          { number of channels (i.e. mono, stereo, etc.) }
              nSamplesPerSec: DWORD;  { sample rate }
              nAvgBytesPerSec: DWORD; { for buffer estimation }
              nBlockAlign: Word;      { block size of data }
              wBitsPerSample: Word;   { number of bits per sample of mono data }
              cbSize: Word;           { the count in bytes of the size of }
              realbps: Word;    { valid bits per sample }
              speakers: DWORD;  { speaker/channel mapping }
              GUID: array [0..15] of char;
              extended:boolean;
              datapos:DWORD;
              datasize:DWORD;
              numofpoints:integer;
             end;


type TWavParser = class
                  private
                    fFilename:string;
                    fIsError:boolean;
                    fErrorstring:string;
                    fOpened:boolean;
                    fError:boolean;
                    fFilestream:TFileStream;
                    fMap:TMappedFile;
                    fMappedPointer:Pointer;
                    fMappedSize:integer;
                    procedure RaiseError(const errorstring_:string);
                    function WriteExtendedHeader:boolean;
                    function WriteStandardHeader:boolean;
                  public
                    WavHeader:TMyWavHeader;
                    property Filename:string read fFilename;
                    property ErrorString:string read fErrorstring;

                    procedure FillStandardHeader(channels_:integer;
                              bps:integer; bits:integer);

                    function OpenWAV(filename_:string):boolean;
                    procedure CloseWav;
                    function ProcessHeader:boolean;
                    function ReadMonoDataToArray(var data_:array of double):boolean;
                    function ReadToIntegers(var data_:array of integer):boolean;
                    function ReadToFloats(var data_:array of single;
                             num:DWORD; offset:DWORD):integer; overload;
                    function ReadToFloats(var data_:array of single;
                             num:DWORD):integer; overload;
                    function ReadRawData(buf:Pointer):boolean;

                    function WriteHeader:boolean;

                    function SaveStandardWAV(const filename_:string; var data:array of double):boolean;
                    function SaveStandardWAVMulti(const filename_:string; var data:array of double):boolean;
                    function SaveWAVFromIntegers(const filename_:string; var data:array of integer):boolean;
                    procedure WriteBlock(source:Pointer; size:integer);

                    function StartSaveBlocks(const filename_:string):boolean;
                    function WriteFloatsBlock(const data_:array of single; num:DWORD):integer;
                    procedure FinalizeFile;

                    function MapWavData:boolean;
                    procedure UnmapWavData;

                    function Normalize(v:byte):double; overload;
                    function Normalize(v:smallint):double; overload;
                    function Normalize(v:T24Bit):double; overload;
                    function Norm(var v):double;

                    constructor Create;
                    destructor Destroy; override;
                    property Map:Pointer read FMappedPointer;
                    property MapSize: integer read FMappedSize;
                  end;

function From24Bit(var P:T24Bit):integer;


implementation


function From24Bit(var P:T24Bit):integer;
begin
  Result := P[2]shl 24 + P[1]shl 16 + P[0]shl 8;
end;



//==============================================================================
//  TWavParser
//==============================================================================

//------------------------------------------------------------------------------
constructor TWavParser.Create;
begin
  inherited Create;
  fIsError := false;
  fOpened := false;
  fFilestream := nil;
  fMap := nil;
  fMappedPointer := nil;
  fMappedSize :=0;
end;

//------------------------------------------------------------------------------
destructor TWavParser.Destroy;
begin
  if Assigned(fFilestream) then fFilestream.Free;
  if Assigned(fMap) then fMap.Free;
  inherited Destroy;
end;

//------------------------------------------------------------------------------
procedure TWavParser.RaiseError(const errorstring_:string);
begin
  fIsError := true;
  fErrorstring := errorstring_+#0+#0;
end;

//------------------------------------------------------------------------------
function TWavParser.OpenWAV(filename_:string):boolean;
begin
  fOpened := false;
  fError := false;
  Result := false;
  try
    fFilestream := TFileStream.Create(fileName_,fmOpenRead OR fmShareDenyNone);
  except
   on EFOpenError do
    begin
      RaiseError('Can''t open file');
      exit;
    end
  else
    begin
     RaiseError('Error while opening the file');
     exit;
    end;
  end;
  fOpened := true;
  fError := false;
  fFilename := fileName_;
  Result := true;
end;

//------------------------------------------------------------------------------
procedure TWavParser.CloseWav;
begin
  fOpened := false;
  fError := false;
  if Assigned(fFilestream) then
  begin
    fFilestream.Free;
    fFileStream:=nil;
  end;
end;

//------------------------------------------------------------------------------
procedure TWavParser.FillStandardHeader(channels_:integer;
                              bps:integer; bits:integer);

begin
  with WavHeader do
  begin
    wFormatTag := 1;
    nChannels := channels_;
    nSamplesPerSec := bps;
    wBitsPerSample := bits;
    nAvgBytesPerSec := nChannels*wBitsPerSample*nSamplesPerSec div 8;
    nBlockAlign := nChannels*wBitsPerSample div 8;
    realbps := wBitsPerSample;
  end;
end;

//------------------------------------------------------------------------------
function TWavParser.ProcessHeader:boolean;
var head:array[0..3] of char;
    fsize:integer;
    frmt:word;
    chnksize:dword;
    datafound:boolean;
begin
  Result := false;
  fError := false;

  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  fFileStream.Seek(0,soFromBeginning);
  fFilestream.Read(head,4);
  if head<>'RIFF' then
  begin
    RaiseError('Wrong format or bad file header (RIFF)');
    exit;
  end;

  fFilestream.Read(fsize,4);
  fsize:=fsize+8;
  if fFilestream.Size<>fsize then
  begin
    RaiseError('Wrong file size or bad header');
    exit;
  end;

  fFilestream.Read(head,4);
  if head<>'WAVE' then
  begin
    RaiseError('Wrong format or bad file header (WAVE)');
    exit;
  end;

  fFilestream.Read(head,4);
  if head<>'fmt ' then
  begin
    RaiseError('Wrong format or bad file header (fmt )');
    exit;
  end;

  fFilestream.Read(chnksize,4);
  fFilestream.Read(frmt,2);

  // --- Standard PCM file
  if (frmt=1) then
  begin
    WavHeader.extended:=false;
    if (chnksize<>16)and(chnksize<>20) then
    begin
      RaiseError('Wrong format or bad file header (fmt chunk size)');
      exit;
    end;
    with WavHeader do
    begin
      wFormatTag:=frmt;
      fFileStream.Read(nChannels,2);
      fFileStream.Read(nSamplesPerSec,4);
      fFileStream.Read(nAvgBytesPerSec,4);
      fFileStream.Read(nBlockAlign,2);
      fFileStream.Read(wBitsPerSample,2);
      if (nChannels*wBitsPerSample*nSamplesPerSec/8)<>nAvgBytesPerSec then
        RaiseError('Bad file header (AvgBytesPerSec)')
      else
      if (wBitsPerSample<>8)and(wBitsPerSample<>16)and
            (wBitsPerSample<>24) then
        RaiseError('Wrong bits per sample (8,16 and 24 allowed)')
      else
      if (nChannels<>1)and(nChannels<>2)then
        RaiseError('Only mono and stereo allowed');
      if fError then exit;
    end;
  end

  // --- Extensible file header
  else if (frmt=$FFFE)and(chnksize=40) then
  begin
    WavHeader.extended:=true;
    with WavHeader do
    begin
      wFormatTag:=frmt;
      fFilestream.Read(nChannels,2);
      fFilestream.Read(nSamplesPerSec,4);
      fFilestream.Read(nAvgBytesPerSec,4);
      fFilestream.Read(nBlockAlign,2);
      fFilestream.Read(wBitsPerSample,2);
      fFilestream.Read(cbSize,2);
      fFilestream.Read(realbps,2);
      if (nChannels*wBitsPerSample*nSamplesPerSec/8)<>nAvgBytesPerSec then
          RaiseError('Bad file header (AvgBytesPerSec)')
      else
      if cbSize<>22 then
        RaiseError('Bad file header (extension chink size)')
      else
      if (realbps<>8)and(realbps<>16)and
            (realbps<>24) then
        RaiseError('Wrong bits per sample (8,16 and 24 allowed)')
      else
      if (nChannels<>1)and(nChannels<>2)then
        RaiseError('Only mono and stereo allowed');
      if fError then exit;

      fFilestream.Read(speakers,4);
      fFilestream.Read(GUID,16);
      if GUID<>TEST_GUID then
      begin
        RaiseError('Non supported format (GUID)');
        exit;
      end;
    end;
  end
  else
  begin
    RaiseError('Non supported WAV format');
    exit;
  end;

  // --- Search for data chunk
  if chnksize=20 then fFilestream.Read(head,4);
  datafound:=false;
  while (not datafound)and(fFilestream.Position<(fFilestream.Size-1)) do
  begin
    fFilestream.Read(head,4);
    fFilestream.Read(chnksize,4);
    WavHeader.datapos:=fFilestream.Position;
    WavHeader.datasize:=chnksize;
    fFilestream.Seek(chnksize,soFromCurrent);
    if head='data' then datafound:=true;
  end;
  if not datafound then
  begin
    RaiseError('No data chunk found');
    exit;
  end;
  WavHeader.numofpoints := WavHeader.datasize div WavHeader.nBlockAlign;

  fFilestream.Seek(WavHeader.datapos,soFromBeginning);

  fError := false;
  Result := true;
end;

//------------------------------------------------------------------------------
function TWavParser.ReadMonoDataToArray(var data_:array of double):boolean;
var i:integer;
    b:byte;      // 8 bit
    s:smallint;  // 16 bit
    l:longint;   // 32 bit
begin
  Result := false;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  fFilestream.Seek(WavHeader.datapos,soFromBeginning);

  // --- 8 bit
  if (WavHeader.wBitsPerSample=8)and(WavHeader.nChannels=1) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(b,1);
      data_[i]:= (b-MAX_SHORTINT+1)/MAX_SHORTINT;
    end;
  end else
  if (WavHeader.wBitsPerSample=8)and(WavHeader.nChannels=2) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(b,1);
      data_[i]:= (b-MAX_SHORTINT+1)/MAX_SHORTINT;
      fFilestream.Seek(1,soFromCurrent);
    end;
  end else
  // --- 16 bit
  if (WavHeader.wBitsPerSample=16)and(WavHeader.nChannels=1) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(s,2);
      data_[i]:=s/MAX_SMALLINT;
    end;
  end else
  if (WavHeader.wBitsPerSample=16)and(WavHeader.nChannels=2) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(s,2);
      data_[i]:=s/MAX_SMALLINT;
      fFilestream.Seek(2,soFromCurrent);
    end;
  end else
  // --- 24 bit standard (3 bytes per sample (1 channel))
  if (not WavHeader.extended)and
      (WavHeader.wBitsPerSample=24)and(WavHeader.nChannels=1) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(l,3);
      l := (l shl 8);
      data_[i]:=l/MAX_24BIT;
    end;
  end else
  if (not WavHeader.extended)and
      (WavHeader.wBitsPerSample=24)and(WavHeader.nChannels=2) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(l,3);
      l := (l shl 8);
      data_[i]:=l/MAX_24BIT;
      fFilestream.Seek(3,soFromCurrent);
    end;
  end else
  // --- 24 bit extended
  if (WavHeader.extended)and
      (WavHeader.wBitsPerSample=24)and(WavHeader.nChannels=1) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(l,3);
      l := (l shl 8);
      data_[i]:=l/MAX_24BIT;
    end;
  end else
  if (WavHeader.extended)and
      (WavHeader.wBitsPerSample=24)and(WavHeader.nChannels=2) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      fFilestream.Read(l,3);
      l := (l shl 8);
      data_[i]:=l/MAX_24BIT;
      fFilestream.Seek(WavHeader.nBlockAlign div 2,soFromCurrent);
    end;
  end
  else
  begin
    RaiseError('Wrong data format (sps/bps/align etc)');
    exit;
  end;
  fError := false;
  Result:=true;
end;

//------------------------------------------------------------------------------
function TWavParser.ReadToFloats(var data_:array of single;
                             num:DWORD; offset:DWORD):integer;
var i,j:integer;
    b:byte;      // 8 bit
    s:smallint;  // 16 bit
    l:longint;   // 32 bit
    step_:DWORD;
    bytes_read:integer;
begin
  Result := -1;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  if integer(offset)>=WavHeader.numofpoints then
  begin
    RaiseError('Offset beyond the end of file');
    exit;
  end;
  if (num+offset)>DWORD(WavHeader.numofpoints) then num:=DWORD(WavHeader.numofpoints)-offset;

  if WavHeader.extended then
    step_ := WavHeader.realbps*WavHeader.nChannels div 8
  else
    step_ := WavHeader.nBlockAlign;

  fFilestream.Seek(WavHeader.datapos+step_*offset,soFromBeginning);

  bytes_read:=0;

  // --- 8 bit               â
  if (WavHeader.wBitsPerSample=8) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(b,1);
        data_[i*WavHeader.nChannels+j]:= (b-MAX_SHORTINT+1)/MAX_SHORTINT;
      end;
  end else
  // --- 16 bit
  if (WavHeader.wBitsPerSample=16) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(s,2);
        data_[i*WavHeader.nChannels+j]:=s/MAX_SMALLINT;
      end;
  end else
  // --- 24 bit standard (3 bytes per sample)
  if (not WavHeader.extended)and
      (WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end else
  // --- 24 bit extended
  if (WavHeader.extended)and(WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end else
  // --- 32 bit
  if (WavHeader.wBitsPerSample=32) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,4);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end
  else
  begin
    RaiseError('Wrong data format (sps/bps/align etc)');
    exit;
  end;
  fError := false;
  Result := bytes_read div integer(step_);
end;

//------------------------------------------------------------------------------
function TWavParser.ReadToFloats(var data_:array of single;
                             num:DWORD):integer;
var i,j:integer;
    b:byte;      // 8 bit
    s:smallint;  // 16 bit
    l:longint;   // 32 bit
    step_:DWORD;
    bytes_read:integer;
begin
  Result := -1;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;


  if (num*WavHeader.nBlockAlign + DWORD(fFilestream.Position))>(WavHeader.datapos+WavHeader.datasize) then
    num:=((WavHeader.datapos+WavHeader.datasize)-fFilestream.Position) div WavHeader.nBlockAlign;

  if WavHeader.extended then
    step_ := WavHeader.realbps*WavHeader.nChannels div 8
  else
    step_ := WavHeader.nBlockAlign;

  bytes_read:=0;

  // --- 8 bit               â
  if (WavHeader.wBitsPerSample=8) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(b,1);
        data_[i*WavHeader.nChannels+j]:= (b-MAX_SHORTINT+1)/MAX_SHORTINT;
      end;
  end else
  // --- 16 bit
  if (WavHeader.wBitsPerSample=16) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(s,2);
        data_[i*WavHeader.nChannels+j]:=s/MAX_SMALLINT;
      end;
  end else
  // --- 24 bit standard (3 bytes per sample)
  if (not WavHeader.extended)and
      (WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end else
  // --- 24 bit extended
  if (WavHeader.extended)and(WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end else
  // --- 32 bit
  if (WavHeader.wBitsPerSample=32) then
  begin
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        bytes_read := bytes_read+fFilestream.Read(l,4);
        data_[i*WavHeader.nChannels+j]:=l/MAX_24BIT;
      end;
  end
  else
  begin
    RaiseError('Wrong data format (sps/bps/align etc)');
    exit;
  end;
  fError := false;
  Result := bytes_read div integer(step_);
end;


//------------------------------------------------------------------------------
function TWavParser.ReadToIntegers(var data_:array of integer):boolean;
var i,j:integer;
    b:byte;      // 8 bit
    s:smallint;  // 16 bit
    l:longint;   // 32 bit
begin
  Result := false;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  fFilestream.Seek(WavHeader.datapos,soFromBeginning);

  // --- 8 bit
  if (WavHeader.wBitsPerSample=8) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(b,1);
        data_[i*WavHeader.nChannels+j]:= (b-MAX_SHORTINT+1);
      end;
  end else
  // --- 16 bit
  if (WavHeader.wBitsPerSample=16) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(s,2);
        data_[i*WavHeader.nChannels+j]:=s;
      end;
  end else
  // --- 24 bit standard (3 bytes per sample (1 channel))
  if (not WavHeader.extended)and(WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l div 256;
      end;
  end else
  // --- 24 bit extended
  if (WavHeader.extended)and(WavHeader.wBitsPerSample=24) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(l,3);
        l := (l shl 8);
        data_[i*WavHeader.nChannels+j]:=l div 256;
      end;
  end else
  // --- 32 bit standard 
  if (not WavHeader.extended)and(WavHeader.wBitsPerSample=32) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(l,4);
        data_[i*WavHeader.nChannels+j]:=l;
      end;
  end else
  // --- 32 bit extended
  if (WavHeader.extended)and(WavHeader.wBitsPerSample=32) then
  begin
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        fFilestream.Read(l,4);
        data_[i*WavHeader.nChannels+j]:=l;
      end;
  end
  else
  begin
    RaiseError('Wrong data format (sps/bps/align etc)');
    exit;
  end;
  fError := false;
  Result:=true;
end;


//------------------------------------------------------------------------------
function TWavParser.ReadRawData(buf:Pointer):boolean;
begin
  Result := false;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  fFilestream.Seek(WavHeader.datapos,soFromBeginning);

  fFileStream.Read(buf^,WavHeader.datasize);

  fError := false;
  Result:=true;
end;


//------------------------------------------------------------------------------
function TWavParser.WriteHeader:boolean;
begin
  if WavHeader.wFormatTag=1 then Result := WriteStandardHeader else
  if WavHeader.wFormatTag=$FFFE then Result := WriteExtendedHeader else
    Result := false;
end;


//------------------------------------------------------------------------------
function TWavParser.WriteStandardHeader:boolean;
var head:array[0..3] of char;
begin
  fError := false;

  WavHeader.RIFFtag := 'RIFF';
  WavHeader.Filesize := WavHeader.datasize+44-8;
  WavHeader.WAVEtag := 'WAVE';
  WavHeader.FMTtag  := 'fmt ';
  WavHeader.chnksize:=16;

  WavHeader.extended:=false;
  fFilestream.Write(WavHeader,36);

  // --- Record data chunk
  head:='data';
  fFilestream.Write(head,4);
  fFilestream.Write(WavHeader.datasize,4);

  fError := false;
  Result := true;
end;

//------------------------------------------------------------------------------
function TWavParser.WriteExtendedHeader:boolean;
var head:array[0..3] of char;
    i:integer;
begin
  fError := false;

  WavHeader.RIFFtag  := 'RIFF';
  WavHeader.Filesize := WavHeader.datasize+60;
  WavHeader.WAVEtag  := 'WAVE';
  WavHeader.FMTtag   := 'fmt ';
  WavHeader.chnksize := 40;
  WavHeader.cbSize   := 0;
  WavHeader.realbps  := WavHeader.wBitsPerSample;
  WavHeader.speakers := 0;
  for i:=0 to 15 do
      WavHeader.GUID[i] := TEST_GUID[i];

  WavHeader.extended:=true;
  fFileStream.Write(WavHeader,60);

  // --- Record data chunk
  head:='data';
  fFilestream.Write(head,4);
  fFilestream.Write(WavHeader.datasize,4);

  fError := false;
  Result := true;
end;


//------------------------------------------------------------------------------
function TWavParser.SaveStandardWAV(const filename_:string; var data:array of double):boolean;
var i:integer;
    fh:integer;
    sample:integer;
begin
  Result := false;

  if FileExists(filename_) then
  begin
    DeleteFile(PChar(filename_));
  end;

  fh := FileCreate(filename_);
  FileClose(fh);

  try
    fFilestream := TFileStream.Create(fileName_,fmOpenWrite OR fmShareDenyNone);
  except
   on EFOpenError do
    begin
      RaiseError('Can''t open file');
      exit;
    end
  else
    begin
     RaiseError('Error while opening the file');
     exit;
    end;
  end;

  WriteHeader;

  // --- 8 bit mono ---
  if WavHeader.wBitsPerSample=8 then
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      sample := Round(data[i]*MAX_SHORTINT+MAX_SHORTINT-1);
      fFilestream.Write(sample,1);
    end
  // --- 16 bit mono ---
  else if WavHeader.wBitsPerSample=16 then
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      sample := Round(data[i]*(MAX_SMALLINT-1));
      fFilestream.Write(sample,2);
    end
  // --- 24 bit mono ---
  else if WavHeader.wBitsPerSample=24 then
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      sample := Round(data[i]*MAX_24BIT) shr 8;
      fFilestream.Write(sample,3);
    end
  // --- 32 bit mono ---
  else if WavHeader.wBitsPerSample=32 then
    for i:=0 to WavHeader.numofpoints-1 do
    begin
      sample := Round(data[i]*MAX_24BIT);
      fFilestream.Write(sample,4);
    end;


  fFilestream.Free;
  fFilestream := nil;

  fError := false;
  Result:=true;
end;


//------------------------------------------------------------------------------
function TWavParser.SaveStandardWAVMulti(const filename_:string; var data:array of double):boolean;
var i,j:integer;
    fh:integer;
    sample:integer;
begin
  Result := false;

  if FileExists(filename_) then
  begin
    DeleteFile(PChar(filename_));
  end;

  fh := FileCreate(filename_);
  FileClose(fh);

  try
    fFilestream := TFileStream.Create(fileName_,fmOpenWrite OR fmShareDenyNone);
  except
   on EFOpenError do
    begin
      RaiseError('Can''t open file');
      exit;
    end
  else
    begin
     RaiseError('Error while opening the file');
     exit;
    end;
  end;

  WriteHeader;

  // --- 8 bit ---
  if WavHeader.wBitsPerSample=8 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data[i*WavHeader.nChannels+j]*MAX_SHORTINT+MAX_SHORTINT-1);
        fFilestream.Write(sample,1);
      end
  // --- 16 bit mono ---
  else if WavHeader.wBitsPerSample=16 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data[i*WavHeader.nChannels+j]*(MAX_SMALLINT-1));
        fFilestream.Write(sample,2);
      end
  // --- 24 bit mono ---
  else if WavHeader.wBitsPerSample=24 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data[i*WavHeader.nChannels+j]*MAX_24BIT) shr 8;
        fFilestream.Write(sample,3);
      end
  // --- 32 bit mono ---
  else if WavHeader.wBitsPerSample=32 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data[i*WavHeader.nChannels+j]*MAX_24BIT);
        fFilestream.Write(sample,4);
      end;


  fFilestream.Free;
  fFilestream := nil;

  fError := false;
  Result:=true;
end;



//------------------------------------------------------------------------------
function TWavParser.SaveWAVFromIntegers(const filename_:string; var data:array of integer):boolean;
var i,j:integer;
    fh:integer;
    sample:integer;
begin
  Result := false;

  if FileExists(filename_) then
  begin
    DeleteFile(PChar(filename_));
  end;

  fh := FileCreate(filename_);
  FileClose(fh);

  try
    fFilestream := TFileStream.Create(fileName_,fmOpenWrite OR fmShareDenyNone);
  except
   on EFOpenError do
    begin
      RaiseError('Can''t open file');
      exit;
    end
  else
    begin
     RaiseError('Error while opening the file');
     exit;
    end;
  end;

  WriteHeader;

  // --- 8 bit  ---
  if WavHeader.wBitsPerSample=8 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := data[i*WavHeader.nChannels+j];
        fFilestream.Write(sample,1);
      end
  // --- 16 bit  ---
  else if WavHeader.wBitsPerSample=16 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := data[i*WavHeader.nChannels+j];
        fFilestream.Write(sample,2);
      end
  // --- 24 bit  ---
  else if WavHeader.wBitsPerSample=24 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := data[i*WavHeader.nChannels+j];
        fFilestream.Write(sample,3);
      end
  // --- 32 bit  ---
  else if WavHeader.wBitsPerSample=32 then
    for i:=0 to WavHeader.numofpoints-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := data[i*WavHeader.nChannels+j];
        fFilestream.Write(sample,4);
      end;

  fFilestream.Free;
  fFilestream := nil;

  fError := false;
  Result:=true;
end;


//------------------------------------------------------------------------------
function TWavParser.StartSaveBlocks(const filename_:string):boolean;
var fh:integer;
begin
  Result := false;

  if FileExists(filename_) then
  begin
    DeleteFile(PChar(filename_));
  end;

  fh := FileCreate(filename_);
  FileClose(fh);

  try
    fFilestream := TFileStream.Create(fileName_,fmOpenWrite OR fmShareDenyNone);
  except
   on EFOpenError do
    begin
      RaiseError('Can''t open file');
      exit;
    end
  else
    begin
     RaiseError('Error while opening the file');
     exit;
    end;
  end;

  WriteHeader;

  Result:=true;
end;

//------------------------------------------------------------------------------
function TWavParser.WriteFloatsBlock(const data_:array of single; num:DWORD):integer;
var i,j:integer;
    sample:integer;
    bytes_written:integer;
    step_:integer;
begin
  bytes_written := 0;

  // --- 8 bit ---
  if WavHeader.wBitsPerSample=8 then
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data_[i*WavHeader.nChannels+j]*MAX_SHORTINT+MAX_SHORTINT-1);
        bytes_written := bytes_written+fFilestream.Write(sample,1);
      end
  // --- 16 bit ---
  else if WavHeader.wBitsPerSample=16 then
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data_[i*WavHeader.nChannels+j]*(MAX_SMALLINT-1));
        bytes_written := bytes_written+fFilestream.Write(sample,2);
      end
  // --- 24 bit ---
  else if WavHeader.wBitsPerSample=24 then
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin
        sample := Round(data_[i*WavHeader.nChannels+j]*MAX_24BIT) shr 8;
        bytes_written := bytes_written+fFilestream.Write(sample,3);
      end
  // --- 32 bit ---
  else if WavHeader.wBitsPerSample=32 then
    for i:=0 to num-1 do
      for j:=0 to WavHeader.nChannels-1 do
      begin                                          
        sample := Round(data_[i*WavHeader.nChannels+j]*MAX_24BIT);
        bytes_written := bytes_written+fFilestream.Write(sample,4);
      end;

  if WavHeader.extended then
    step_ := WavHeader.realbps*WavHeader.nChannels div 8
  else
    step_ := WavHeader.nBlockAlign;
  Result := bytes_written div (step_);
end;

//------------------------------------------------------------------------------
procedure TWavParser.FinalizeFile;
begin
  CloseWAV;
end;


//------------------------------------------------------------------------------
function TWavParser.MapWavData:boolean;
var p:pointer;
begin
  Result := false;
  if not fOpened then
  begin
    RaiseError('File not opened');
    exit;
  end;

  if Assigned(FMap) then FMap.Free;

  ProcessHeader;

  try
    FMap := TMappedFile.Create(fFilename);
    p := Pointer(DWORD(FMap.Content)+WavHeader.datapos);
    fMappedPointer := p;
    fMappedSize := FMap.Size;
  except
    begin
      FMap.Free;
      fMappedPointer := nil;
      fMappedSize := 0;
      RaiseError('Error mapping file');
      exit;
    end;
  end;

  Result := true;
end;

//------------------------------------------------------------------------------
procedure TWavParser.UnmapWavData;
begin
  if Assigned(FMap) then FMap.Free;
end;

//------------------------------------------------------------------------------
function TWavParser.Normalize(v:byte):double;
begin
  Result := (v-MAX_SHORTINT)/MAX_SHORTINT;
end;
//------------------------------------------------------------------------------
function TWavParser.Normalize(v:smallint):double;
begin
  Result := v/MAX_SMALLINT;
end;
//------------------------------------------------------------------------------
function TWavParser.Normalize(v:T24Bit):double;
begin
  Result := From24Bit(v)/MAX_24BIT;
end;
//------------------------------------------------------------------------------
function TWavParser.Norm(var v):double;
begin
  Result := 0;
  if not fOpened then exit;

  if (WavHeader.wBitsPerSample=8) then
    Result := Normalize(byte(v))
  else if (WavHeader.wBitsPerSample=16) then
    Result := Normalize(smallint(v))
  else if (not WavHeader.extended)and(WavHeader.wBitsPerSample=24) then
    Result := Normalize(T24Bit(v))
  else if (WavHeader.extended)and (WavHeader.wBitsPerSample=24) then
    Result := Normalize(T24Bit(v))
  else
    Result := 0;
end;

//------------------------------------------------------------------------------
procedure TWavParser.WriteBlock(source:Pointer; size:integer);
begin
  fFileStream.Write(source^,size);
end;

end.
