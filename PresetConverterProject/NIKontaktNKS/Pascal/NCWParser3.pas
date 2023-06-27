unit NCWParser3;

interface

uses SysUtils, Classes, Windows, WAVParser3, BitProcess3;

const NCW_SIGNATURE1:array [0..7] of byte = ($01,$A8,$9E,$D6,$31,$01,$00,$00);
const NCW_SIGNATURE2:array [0..7] of byte = ($01,$A8,$9E,$D6,$30,$01,$00,$00);
const BLOCK_SIGNATURE:array[0..3] of byte = ($16,$0C,$9A,$3E);

const NCW_SAMPLES = 512;
const MAX_CHANNELS = 6;

type TNCWHeader = packed record
            Signature:array [0..7] of byte;
            Channels:WORD;
            Bits:WORD;
            Samplerate:DWORD;
            numSamples:DWORD;
            block_def_offset:DWORD;
            blocks_offset:DWORD;
            blocks_size:DWORD;
            some_data:array [0..87] of char;
          end;

type TBlockHeader = packed record
            Signature:array [0..3] of char;
            BaseValue:integer;
            bits:smallint;
            flags:WORD;
            zeros2:DWORD;
          end;
type PBlockHeader = ^TBlockHeader;



type TNCWParser = class
            private
              procedure ReadNCW8;
              procedure ReadNCW16;
              procedure ReadNCW24;
              procedure ReadNCW32;
              procedure WriteNCW8(const WavHeader:TMyWAVHeader);
              procedure WriteNCW16(const WavHeader:TMyWAVHeader);
              procedure WriteNCW24(const WavHeader:TMyWAVHeader);
              procedure WriteNCW32(const WavHeader:TMyWAVHeader);
            public
              fs:TFileStream;
              Header:TNCWHeader;
              BlocksDefList:array of DWORD;
              datai:array of integer;
              data8:array of shortint;
              data16:array of smallint;
              data24:array of int24;
              ms:TMemoryStream;
              constructor Create;
              destructor Destroy; override;
              procedure Clear;
              procedure OpenNCWFile(const filename:string);
              procedure CloseFile;
              procedure ReadNCWintegers;
              procedure ReadNCW;

              procedure WriteNCWintegers(const WavHeader:TMyWAVHeader);
              procedure WriteNCW(const WavHeader:TMyWAVHeader);
              procedure SaveToWAVintegers(const filename:string);
              procedure SaveToWAV(const filename:string);
              procedure SaveToWAVEx(const filename:string);
              procedure SaveToNCW(const filename:string);
            end;

function MinBits(x:integer):integer;


implementation

uses Math;


//==============================================================================
constructor TNCWParser.Create;
begin
  inherited Create;
  fs := nil;
  ms := nil;
end;

//==============================================================================
destructor TNCWParser.Destroy;
begin
  Clear;
  inherited Destroy;
end;

//==============================================================================
procedure TNCWParser.Clear;
begin
  CloseFile;
  SetLength(datai,0);
  SetLength(data8,0);
  SetLength(data16,0);
  SetLength(data24,0);
  if Assigned(ms) then begin ms.Free; ms:=nil; end;
end;

//==============================================================================
procedure TNCWParser.CloseFile;
begin
  if Assigned(fs) then begin fs.Free; fs:=nil; end;
  SetLength(BlocksDefList,0);
end;


//==============================================================================
procedure TNCWParser.OpenNCWFile(const filename:string);
var i:integer;
begin
  //--- Opening ---------------------------
  try
    fs := TFileStream.Create(filename,fmOpenRead or fmShareDenyNone);
  except
    raise Exception.Create('Can''t open file');
  end;
  fs.Read(Header,sizeof(Header));

  //--- Checking file signature -----------
  for i:=0 to 7 do
    if (Header.Signature[i]<>NCW_SIGNATURE1[i])and
       (Header.Signature[i]<>NCW_SIGNATURE2[i]) then
      raise Exception.Create('Wrong file signature');

  //--- Filling blocks list ---------------
  SetLength(BlocksDefList,
      (Header.blocks_offset-Header.block_def_offset)div 4);
  fs.Seek(Header.block_def_offset,soFromBeginning);
  fs.Read(BlocksDefList[0],Length(BlocksDefList)*4);
end;

//==============================================================================
procedure TNCWParser.SaveToWAVintegers(const filename:string);
var wp:TWavParser;
begin
  wp := TWavParser.Create;
  with wp.WavHeader do
  begin
    wFormatTag := 1; // Standard wav
    nChannels := Header.Channels;
    nSamplesPerSec := Header.Samplerate;
    wBitsPerSample := Header.Bits;
    nBlockAlign := nChannels*wBitsPerSample div 8;
    nAvgBytesPerSec := nSamplesPerSec*nBlockAlign;
    cbSize:=0;
    datasize:=nBlockAlign*Header.numSamples;
    numOfPoints := Header.numSamples;
  end;
  wp.SaveWAVFromIntegers(filename,datai);
  wp.Free;
end;

//==============================================================================
procedure ClearMultiByteArray(a:pointer);
type multiar_ =array of array of byte;
var b:multiar_;
    i,j:integer;
begin
  b := a;
  for i:=0 to Length(b)-1 do
    for j:=0 to Length(b[i])-1 do
      b[i,j]:=0;
end;

//==============================================================================
procedure ClearByteArray(a:array of byte);
var i:integer;
begin
  for i:=0 to Length(a)-1 do a[i]:=0;
end;

//==============================================================================
procedure TNCWParser.ReadNCWintegers;
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    tempb:array of array of byte;
    tempi:array [0..4,0..511] of integer;
    curoffset:integer;
    cursample:DWORD;
    nbits:integer;
    nrelative:boolean;
begin
  SetLength(datai,Header.numSamples*Header.Channels);
  curoffset:=0;
  cursample:=0;

  SetLength(tempb,Header.Channels);
  for i:=0 to Header.Channels-1 do
    SetLength(tempb[i],Header.Bits*64);

  ClearMultibyteArray(tempb);
  FillChar(tempi,5*512*sizeof(integer),0);

  for i:=0 to Length(BlocksDefList)-2 do
  begin
    fs.Seek(Header.blocks_offset+BlocksDefList[i],soFromBeginning);
    for j:=0 to Header.Channels-1 do
    begin
      fs.Read(bHeader,sizeof(bHeader));
      if bHeader.bits<0 then
      begin
        nbits:=Abs(bHeader.bits);
        fs.Read(tempb[j,0],nbits*64);
        TMyBits.FillIntegersAbs(NCW_SAMPLES, nbits, @tempb[j,0], bHeader.BaseValue,
                           tempi[j]);
      end else
      begin
        if bHeader.bits=0 then nbits:=Header.Bits
                          else nbits:=bHeader.bits;
        fs.Read(tempb[j,0],nbits*64);
        nrelative := (bHeader.bits<>0);
        TMyBits.FillIntegers(NCW_SAMPLES, nbits, @tempb[j,0], bHeader.BaseValue,
                           tempi[j], nrelative)
      end;
    end;

    if bHeader.flags=1 then
      for k:=0 to NCW_SAMPLES-1 do
      begin
        // Considering stereo samples
        datai[curoffset]:=tempi[0,k]+tempi[1,k];
        Inc(curoffset);
        datai[curoffset]:=tempi[0,k]-tempi[1,k];
        Inc(curoffset);
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
    else
      for k:=0 to NCW_SAMPLES-1 do
      begin
        for j:=0 to Header.Channels-1 do
        begin
          datai[curoffset]:=tempi[j,k];
          Inc(curoffset);
        end;
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
  end;
 ex:
  for i:=0 to Header.Channels-1 do
  begin
    SetLength(tempb[i],0);
  end;
  SetLength(tempb,0);
end;



//==============================================================================
function MinBits(x:integer):integer;
label ex;
var bits:integer;
begin
  if x=0 then
    bits:=2
  else
  if x>0 then
  begin
    bits := 32;
    while bits>2 do
    begin
      x := x shl 1;
      if (x and $80000000)<>0 then goto ex;
      Dec(bits);
    end;
    goto ex;
  end
  else
  begin
    bits := 32;
    while bits>2 do
    begin
      x := x shl 1;
      if (x and $80000000)=0 then goto ex;
      Dec(bits);
    end;
  end;
 ex:
  Result := bits;
end;

//==============================================================================
function FindMinBits(const ar:array of integer):integer;
var i:integer;
    min_,max_:integer;
begin
  min_:=ar[1]; max_:=min_;
  for i:=0 to NCW_SAMPLES-1 do
  begin
    if ar[i]<min_ then min_:= ar[i];
    if ar[i]>max_ then max_:= ar[i];
  end;
  Result := Max(MinBits(min_),MinBits(max_));
end;

//==============================================================================
procedure DifArray(var ars:array of integer; var ard:array of integer);
var i:integer;
begin
  for i:=0 to Length(ars)-2 do
  begin
    ard[i] := ars[i+1]-ars[i];
  end;
  ard[Length(ars)-1] := 0;
end;

//==============================================================================
procedure DifArray8(var ars:array of shortint; var ard:array of shortint;
                    var max_,min_:integer);
var i:integer;
begin
  max_ := -128; min_ := 127;
  for i:=0 to Length(ars)-2 do
  begin
    ard[i] := ars[i+1]-ars[i];
    if ard[i]>max_ then max_:=ard[i];
    if ard[i]<min_ then min_:=ard[i];
  end;
  ard[Length(ars)-1] := 0;
end;

//==============================================================================
procedure DifArray16(var ars:array of smallint; var ard:array of smallint;
                     var max_,min_:integer);
var i:integer;
begin
  max_ := -32768; min_ := 32767;
  for i:=0 to Length(ars)-2 do
  begin
    ard[i] := ars[i+1]-ars[i];
    if ard[i]>max_ then max_:=ard[i];
    if ard[i]<min_ then min_:=ard[i];
  end;
  ard[Length(ars)-1] := 0;
end;


//==============================================================================
procedure DifArray24(var ars:array of int24; var ard:array of int24;
                     var max_,min_:integer);
var i:integer;
    ti1,ti2:integer;
begin
  max_ := integer($80000000);
  min_ := 2147483647;

  for i:=0 to Length(ars)-2 do
  begin
    ti1 := ((ars[i][0]) or (ars[i][1]shl 8) or (ars[i][2]shl 16))shl 8;
    ti2 := ((ars[i+1][0]) or (ars[i+1][1]shl 8) or (ars[i+1][2]shl 16))shl 8;
    ti2 := (ti2-ti1);
    if ti2>max_ then max_:=ti2;
    if ti2<min_ then min_:=ti2;
    ti2 := ti2 shr 8;
    ard[i][0] := (ti2) and $FF;
    ard[i][1] := (ti2 shr 8) and $FF;
    ard[i][2] := (ti2 shr 16) and $FF;

    //ard[i] := ars[i+1]-ars[i];
  end;
  if max_<0 then max_:= ((DWORD(max_) shr 8)or $FF000000)
            else max_:=(max_ shr 8);
  if min_<0 then min_:=(DWORD(min_) shr 8)or $FF000000
            else min_:=(min_ shr 8);
  //ard[Length(ars)-1] := 0;
    ard[Length(ars)-1][0] := 0;
    ard[Length(ars)-1][1] := 0;
    ard[Length(ars)-1][2] := 0;
end;

//==============================================================================
procedure DifArray32(var ars:array of integer; var ard:array of integer;
                     var max_,min_:integer);
var i:integer;
begin
  max_ := integer($80000000);
  min_ := 2147483647;
  for i:=0 to Length(ars)-2 do
  begin
    ard[i] := ars[i+1]-ars[i];
    if ard[i]>max_ then max_:=ard[i];
    if ard[i]<min_ then min_:=ard[i];
  end;
  ard[Length(ars)-1] := 0;
end;


//==============================================================================
procedure FillBlockHeader(var bheader:TBlockHeader);
begin
  CopyMemory(@bheader.Signature,@BLOCK_SIGNATURE,4);
  bheader.flags:=0;
  bheader.zeros2:=0;
end;


//==============================================================================
procedure TNCWParser.WriteNCWintegers(const WavHeader:TMyWAVHeader);
label ex;
var i,j:integer;
    bHeader:TBlockHeader;
    tempi:array [0..4,0..NCW_SAMPLES-1] of integer;
    tempidif:array [0..4,0..NCW_SAMPLES-1] of integer;
    tempb:array of byte;
    nbits:integer;
    curblocknumber:integer;
    nblocks:integer;
    blocksize:integer;
    curblockoffset:integer;
begin

  // --- Fill header ----
  with Header do
  begin
    CopyMemory(@Signature,
    @NCW_SIGNATURE1,sizeof(Signature));
    Channels := wavHeader.nChannels;
    Bits := wavHeader.wBitsPerSample;
    Samplerate := wavHeader.nSamplesPerSec;
    numSamples := wavHeader.numofpoints;
    FillChar(some_data,sizeof(some_data),0);
    block_def_offset := $78;
    nblocks := numSamples div 512 + 1;
    if (numSamples mod 512)<>0 then Inc(nblocks);
    blocks_offset := block_def_offset + DWORD(nblocks)*4;
  end;

  ms := TMemoryStream.Create;
  SetLength(BlocksDefList,nblocks);

  curblockoffset := 0;

  // --- Handling data by blocks ----
  for curblocknumber:=0 to nblocks-2 do
  begin
    BlocksDefList[curblocknumber] := curblockoffset;
    // --- Fill 512 samples arrays ----
    for i:=0 to NCW_SAMPLES-1 do
      for j:=0 to Header.Channels-1 do
      begin
        if (curblocknumber*512*Header.Channels+i*Header.Channels+j)<Length(datai) then
          tempi[j,i] := datai[curblocknumber*512*Header.Channels+i*Header.Channels+j]
        else
          tempi[j,i] := 0;
      end;


    // --- Consequential array handling  ----
    for j:=0 to Header.Channels-1 do
    begin
      DifArray(tempi[j],tempidif[j]);           //--- Find differences --
      nbits := FindMinBits(tempidif[j]);           //--- Find miminal bits --

      FillBlockHeader(bHeader);
      bHeader.BaseValue := tempi[j,0];
      if nbits>=Header.Bits then
      begin
        bHeader.bits:=-Header.Bits;
        nbits := Header.Bits;
      end
      else
        bHeader.bits := nbits;

      blocksize := nbits*64;
      SetLength(tempb,blocksize);
      ClearByteArray(tempb);
      if bHeader.bits<0 then
        TMyBits.FillBitsAbs(NCW_SAMPLES,nbits,@tempb[0],tempi[j])
      else
        TMyBits.FillBits(NCW_SAMPLES,nbits,@tempb[0],tempidif[j]);
      ms.Write(bHeader,sizeof(bHeader));
      ms.Write(tempb[0],blocksize);
      curblockoffset := curblockoffset + sizeof(bHeader)+blocksize;
    end;
  end;
  BlocksDefList[Length(BlocksDefList)-1] := curblockoffset;
  Header.blocks_size := curblockoffset;
  Header.blocks_offset := sizeof(Header) + Length(BlocksDefList)*4;
end;

//==============================================================================
procedure TNCWParser.SaveToNCW(const filename:string);
var fs:TFileStream;
    i:integer;
begin
  fs := TFileStream.Create(filename,fmCreate or fmShareDenyNone);
  fs.Write(Header,sizeof(Header));
  for i:=0 to Length(BlocksDefList)-1 do
    fs.Write(BlocksDefList[i],4);
  fs.CopyFrom(ms,0);
  fs.Free;
end;

//==============================================================================
procedure TNCWParser.ReadNCW8;
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    input_buf:Pointer;
    temp8:array [0..MAX_CHANNELS-1,0..NCW_SAMPLES-1] of shortint;
    curoffset:integer;
    cursample:DWORD;
    nbits:integer;
    nrelative:boolean;
begin
  SetLength(data8,Header.numSamples*Header.Channels);
  curoffset:=0;
  cursample:=0;

  GetMem(input_buf,Header.Bits*64);
  //FillChar(input_buf^,Header.Bits*64,0);
  //FillChar(temp8,MAX_CHANNELS*NCW_SAMPLES,0);

  for i:=0 to Length(BlocksDefList)-2 do
  begin
    fs.Seek(Header.blocks_offset+BlocksDefList[i],soFromBeginning);
    for j:=0 to Header.Channels-1 do
    begin
      fs.Read(bHeader,sizeof(bHeader));
      if bHeader.bits<0 then
      begin
        nbits:=Abs(bHeader.bits);
        fs.Read(input_buf^,nbits*64);
        TMyBits.Fill8(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp8[j,0],false);
      end else
      begin
        if bHeader.bits=0 then nbits:=Header.Bits
                          else nbits:=bHeader.bits;
        fs.Read(input_buf^,nbits*64);
        nrelative := (bHeader.bits<>0);
        TMyBits.Fill8(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp8[j,0],nrelative);
      end;
    end;

    if bHeader.flags=1 then
      for k:=0 to NCW_SAMPLES-1 do
      begin
        // Considering stereo samples
        data8[curoffset]:=temp8[0,k]+temp8[1,k];
        Inc(curoffset);
        data8[curoffset]:=temp8[0,k]-temp8[1,k];
        Inc(curoffset);
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
    else
      for k:=0 to NCW_SAMPLES-1 do
      begin
        for j:=0 to Header.Channels-1 do
        begin
          data8[curoffset]:=temp8[j,k];
          Inc(curoffset);
        end;
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
  end;
 ex:
  FreeMem(input_buf);
end;

//==============================================================================
procedure TNCWParser.ReadNCW16;
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    input_buf:Pointer;
    temp16:array [0..MAX_CHANNELS-1,0..NCW_SAMPLES-1] of smallint;
    curoffset:integer;
    cursample:DWORD;
    nbits:integer;
    nrelative:boolean;
begin
  SetLength(data16,Header.numSamples*Header.Channels);
  curoffset:=0;
  cursample:=0;

  GetMem(input_buf,Header.Bits*64);
  //FillChar(input_buf^,Header.Bits*64,0);
  //FillChar(temp16,MAX_CHANNELS*NCW_SAMPLES*2,0);

  for i:=0 to Length(BlocksDefList)-2 do
  begin
    fs.Seek(Header.blocks_offset+BlocksDefList[i],soFromBeginning);
    for j:=0 to Header.Channels-1 do
    begin
      fs.Read(bHeader,sizeof(bHeader));
      if bHeader.bits<0 then
      begin
        nbits:=Abs(bHeader.bits);
        fs.Read(input_buf^,nbits*64);
        TMyBits.Fill16(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp16[j,0],false);
      end else
      begin
        if bHeader.bits=0 then nbits:=Header.Bits
                          else nbits:=bHeader.bits;
        fs.Read(input_buf^,nbits*64);
        nrelative := (bHeader.bits<>0);
        TMyBits.Fill16(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp16[j,0],nrelative);
      end;
    end;

    if bHeader.flags=1 then
      for k:=0 to NCW_SAMPLES-1 do
      begin
        // Considering stereo samples
        data16[curoffset]:=temp16[0,k]+temp16[1,k];
        Inc(curoffset);
        data16[curoffset]:=temp16[0,k]-temp16[1,k];
        Inc(curoffset);
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
    else
      for k:=0 to NCW_SAMPLES-1 do
      begin
        for j:=0 to Header.Channels-1 do
        begin
          data16[curoffset]:=temp16[j,k];
          Inc(curoffset);
        end;
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
  end;
 ex:
  FreeMem(input_buf);
end;


//==============================================================================
procedure TNCWParser.ReadNCW24;
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    input_buf:Pointer;
    temp24:array [0..MAX_CHANNELS-1,0..NCW_SAMPLES-1] of int24;
    curoffset:integer;
    cursample:DWORD;
    nbits:integer;
    nrelative:boolean;
    ti1,ti2,ti3:integer;
begin
  SetLength(data24,Header.numSamples*Header.Channels);
  curoffset:=0;
  cursample:=0;

  GetMem(input_buf,Header.Bits*64);
  //FillChar(input_buf^,Header.Bits*64,0);
  //FillChar(temp24,MAX_CHANNELS*NCW_SAMPLES*3,0);

  for i:=0 to Length(BlocksDefList)-2 do
  begin
    fs.Seek(Header.blocks_offset+BlocksDefList[i],soFromBeginning);
    for j:=0 to Header.Channels-1 do
    begin
      fs.Read(bHeader,sizeof(bHeader));
      if bHeader.bits<0 then
      begin
        nbits:=Abs(bHeader.bits);
        fs.Read(input_buf^,nbits*64);
        TMyBits.Fill24(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp24[j,0],false);
      end else
      begin
        if bHeader.bits=0 then nbits:=Header.Bits
                          else nbits:=bHeader.bits;
        fs.Read(input_buf^,nbits*64);
        nrelative := (bHeader.bits<>0);
        TMyBits.Fill24(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp24[j,0],nrelative);
      end;
    end;

    if bHeader.flags=1 then
      for k:=0 to NCW_SAMPLES-1 do
      begin
        // Considering stereo samples
        ti1 := (temp24[0,k][0] + (temp24[0,k][1]shl 8) + (temp24[0,k][2]shl 16))shl 8;
        ti2 := (temp24[1,k][0] + (temp24[1,k][1]shl 8) + (temp24[1,k][2]shl 16))shl 8;
        ti3 := (ti1+ti2);
        data24[curoffset][0] := (ti3 shr 8)and $FF;
        data24[curoffset][1] := (ti3 shr 16)and $FF;
        data24[curoffset][2] := (ti3 shr 24)and $FF;
        Inc(curoffset);

        ti1 := (temp24[0,k][0] + (temp24[0,k][1]shl 8) + (temp24[0,k][2]shl 16))shl 8;
        ti2 := (temp24[1,k][0] + (temp24[1,k][1]shl 8) + (temp24[1,k][2]shl 16))shl 8;
        ti3 := (ti1-ti2);
        data24[curoffset][0] := (ti3 shr 8)and $FF;
        data24[curoffset][1] := (ti3 shr 16)and $FF;
        data24[curoffset][2] := (ti3 shr 24)and $FF;
        Inc(curoffset);

        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
    else
      for k:=0 to NCW_SAMPLES-1 do
      begin
        for j:=0 to Header.Channels-1 do
        begin
          data24[curoffset][0]:=temp24[j,k][0];
          data24[curoffset][1]:=temp24[j,k][1];
          data24[curoffset][2]:=temp24[j,k][2];
          Inc(curoffset);
        end;
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
  end;
 ex:
  FreeMem(input_buf);
end;


//==============================================================================
procedure TNCWParser.ReadNCW32;
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    input_buf:Pointer;
    temp32:array [0..MAX_CHANNELS-1,0..NCW_SAMPLES-1] of integer;
    curoffset:integer;
    cursample:DWORD;
    nbits:integer;
    nrelative:boolean;
begin
  SetLength(datai,Header.numSamples*Header.Channels);
  curoffset:=0;
  cursample:=0;

  GetMem(input_buf,Header.Bits*64);
  //FillChar(input_buf^,Header.Bits*64,0);
  //FillChar(temp32,MAX_CHANNELS*NCW_SAMPLES*4,0);

  for i:=0 to Length(BlocksDefList)-2 do
  begin
    fs.Seek(Header.blocks_offset+BlocksDefList[i],soFromBeginning);
    for j:=0 to Header.Channels-1 do
    begin
      fs.Read(bHeader,sizeof(bHeader));
      if bHeader.bits<0 then
      begin
        nbits:=Abs(bHeader.bits);
        fs.Read(input_buf^,nbits*64);
        TMyBits.Fill32(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp32[j,0],false);
      end else
      begin
        if bHeader.bits=0 then nbits:=Header.Bits
                          else nbits:=bHeader.bits;
        fs.Read(input_buf^,nbits*64);
        nrelative := (bHeader.bits<>0);
        TMyBits.Fill32(NCW_SAMPLES, nbits, input_buf,
                      bHeader.BaseValue,@temp32[j,0],nrelative);
      end;
    end;

    if bHeader.flags=1 then
      for k:=0 to NCW_SAMPLES-1 do
      begin
        // Considering stereo samples
        datai[curoffset]:=temp32[0,k]+temp32[1,k];
        Inc(curoffset);
        datai[curoffset]:=temp32[0,k]-temp32[1,k];
        Inc(curoffset);
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
    else
      for k:=0 to NCW_SAMPLES-1 do
      begin
        for j:=0 to Header.Channels-1 do
        begin
          datai[curoffset]:=temp32[j,k];
          Inc(curoffset);
        end;
        Inc(cursample);
        if cursample>=Header.numSamples then goto ex;
      end
  end;
 ex:
  FreeMem(input_buf);
end;

//==============================================================================
procedure TNCWParser.ReadNCW;
begin
  case Header.Bits of
  8:ReadNCW8;
  16:ReadNCW16;
  24:ReadNCW24;
  32:ReadNCW32;
  end;
end;


//==============================================================================
procedure TNCWParser.SaveToWAV(const filename:string);
var wp:TWavParser;
    block_size:integer;
    nblocks:integer;
    nrem:integer;
    i:integer;
    buf:^byte;
begin
  wp := TWavParser.Create;
  with wp.WavHeader do
  begin
    wFormatTag := 1; // Standard wav
    nChannels := Header.Channels;
    nSamplesPerSec := Header.Samplerate;
    wBitsPerSample := Header.Bits;
    nBlockAlign := nChannels*wBitsPerSample div 8;
    nAvgBytesPerSec := nSamplesPerSec*nBlockAlign;
    cbSize:=0;
    datasize:=nBlockAlign*Header.numSamples;
    numOfPoints := Header.numSamples;
    datapos := 44;
  end;
  wp.StartSaveBlocks(filename);

  block_size := 1024;
  nblocks := integer(wp.WavHeader.datasize) div block_size;
  nrem := integer(wp.WavHeader.datasize) - nblocks*block_size;
  case Header.Bits of
  8:  buf:=@data8[0];
  16: buf:=@data16[0];
  24: buf:=@data24[0];
  32: buf:=@datai[0];
  else
    begin
      Raise Exception.Create('NCWPARSER.SaveToWav: Unsupported BitsPerSample');
      exit;
    end
  end;

  for i:=0 to nblocks-1 do
  begin
    wp.WriteBlock(buf,block_size);
    Inc(buf,block_size);
  end;
  if nrem<>0 then wp.WriteBlock(buf,nrem);
  wp.CloseWav;
  wp.Free;
end;


//==============================================================================
procedure TNCWParser.SaveToWAVEx(const filename:string);
var wp:TWavParser;
    block_size:integer;
    nblocks:integer;
    nrem:integer;
    i:integer;
    buf:^byte;
begin
  wp := TWavParser.Create;
  with wp.WavHeader do
  begin
    wFormatTag := $FFFE; // Extended wav
    nChannels := Header.Channels;
    nSamplesPerSec := Header.Samplerate;
    wBitsPerSample := Header.Bits;
    nBlockAlign := nChannels*wBitsPerSample div 8;
    nAvgBytesPerSec := nSamplesPerSec*nBlockAlign;
    cbSize:=0;
    datasize:=nBlockAlign*Header.numSamples;
    numOfPoints := Header.numSamples;
    datapos := 44;
    wp.WavHeader.cbSize := 0;
    wp.WavHeader.realbps := Header.Bits;
    wp.WavHeader.speakers := 0;
    for i:=0 to 15 do
      wp.WavHeader.GUID[i] := TEST_GUID[i];
  end;
  wp.StartSaveBlocks(filename);

  block_size := 1024;
  nblocks := integer(wp.WavHeader.datasize) div block_size;
  nrem := integer(wp.WavHeader.datasize) - nblocks*block_size;
  case Header.Bits of
  8:  buf:=@data8[0];
  16: buf:=@data16[0];
  24: buf:=@data24[0];
  32: buf:=@datai[0];
  else
    begin
      Raise Exception.Create('NCWPARSER.SaveToWav: Unsupported BitsPerSample');
      exit;
    end
  end;

  for i:=0 to nblocks-1 do
  begin
    wp.WriteBlock(buf,block_size);
    Inc(buf,block_size);
  end;
  if nrem<>0 then wp.WriteBlock(buf,nrem);
  wp.CloseWav;
  wp.Free;
end;

//==============================================================================
procedure TNCWParser.WriteNCW(const WavHeader:TMyWAVHeader);
begin
  case WavHeader.wBitsPerSample of
  8:WriteNcw8(WavHeader);
  16:WriteNcw16(WavHeader);
  24:WriteNcw24(WavHeader);
  32:WriteNcw32(WavHeader);
  else
    exit;
  end;
end;

//==============================================================================
procedure TNCWParser.WriteNCW8(const WavHeader:TMyWAVHeader);
label ex;
var i,j:integer;
    bHeader:TBlockHeader;
    temp8:array [0..4,0..NCW_SAMPLES-1] of shortint;
    temp8dif:array [0..4,0..NCW_SAMPLES-1] of shortint;
    tempb:array of byte;
    nbits:integer;
    curblocknumber:integer;
    nblocks:integer;
    blocksize:integer;
    curblockoffset:integer;
    max_,min_:integer;
begin

  // --- Fill header ----
  with Header do
  begin
    CopyMemory(@Signature,
    @NCW_SIGNATURE1,sizeof(Signature));
    Channels := wavHeader.nChannels;
    Bits := wavHeader.wBitsPerSample;
    Samplerate := wavHeader.nSamplesPerSec;
    numSamples := wavHeader.numofpoints;
    FillChar(some_data,sizeof(some_data),0);
    block_def_offset := $78;
    nblocks := numSamples div 512 + 1;
    if (numSamples mod 512)<>0 then Inc(nblocks);
    blocks_offset := block_def_offset + DWORD(nblocks)*4;
  end;

  ms := TMemoryStream.Create;
  SetLength(BlocksDefList,nblocks);

  curblockoffset := 0;
  SetLength(tempb,Header.Bits*64);

  // --- Handling data by blocks ----
  for curblocknumber:=0 to nblocks-2 do
  begin
    BlocksDefList[curblocknumber] := curblockoffset;
    // --- Fill 512 samples arrays ----
    for i:=0 to NCW_SAMPLES-1 do
      for j:=0 to Header.Channels-1 do
      begin
        if (curblocknumber*512*Header.Channels+i*Header.Channels+j)<Length(data8) then
          temp8[j,i] := data8[curblocknumber*512*Header.Channels+i*Header.Channels+j]
        else
          temp8[j,i] := 0;
      end;


    // --- Consequential array handling  ----
    for j:=0 to Header.Channels-1 do
    begin
      DifArray8(temp8[j],temp8dif[j],max_,min_);   //--- Find differences --
      nbits := Max(MinBits(min_),MinBits(max_));   //--- Find miminal bits --

      FillBlockHeader(bHeader);
      bHeader.BaseValue := temp8[j,0];
      if nbits>=Header.Bits then
      begin
        bHeader.bits:=-Header.Bits;
        nbits := Header.Bits;
      end
      else
        bHeader.bits := nbits;

      blocksize := nbits*64;
      //ClearByteArray(tempb);

      if bHeader.bits<0 then
        TMyBits.Encode_8(NCW_SAMPLES,nbits,@temp8[j,0],@tempb[0])
      else
        TMyBits.Encode_8(NCW_SAMPLES,nbits,@temp8dif[j,0],@tempb[0]);
      ms.Write(bHeader,sizeof(bHeader));
      ms.Write(tempb[0],blocksize);
      curblockoffset := curblockoffset + sizeof(bHeader)+blocksize;
    end;
  end;
  BlocksDefList[Length(BlocksDefList)-1] := curblockoffset;
  Header.blocks_size := curblockoffset;
  Header.blocks_offset := sizeof(Header) + Length(BlocksDefList)*4;
end;

//==============================================================================
procedure TNCWParser.WriteNCW16(const WavHeader:TMyWAVHeader);
label ex;
var i,j:integer;
    bHeader:TBlockHeader;
    temp16:array [0..4,0..NCW_SAMPLES-1] of smallint;
    temp16dif:array [0..4,0..NCW_SAMPLES-1] of smallint;
    tempb:array of byte;
    nbits:integer;
    curblocknumber:integer;
    nblocks:integer;
    blocksize:integer;
    curblockoffset:integer;
    max_,min_:integer;
begin

  // --- Fill header ----
  with Header do
  begin
    CopyMemory(@Signature,
    @NCW_SIGNATURE1,sizeof(Signature));
    Channels := wavHeader.nChannels;
    Bits := wavHeader.wBitsPerSample;
    Samplerate := wavHeader.nSamplesPerSec;
    numSamples := wavHeader.numofpoints;
    FillChar(some_data,sizeof(some_data),0);
    block_def_offset := $78;
    nblocks := numSamples div 512 + 1;
    if (numSamples mod 512)<>0 then Inc(nblocks);
    blocks_offset := block_def_offset + DWORD(nblocks)*4;
  end;

  ms := TMemoryStream.Create;
  SetLength(BlocksDefList,nblocks);

  curblockoffset := 0;
  SetLength(tempb,Header.Bits*64*2);

  // --- Handling data by blocks ----
  for curblocknumber:=0 to nblocks-2 do
  begin
    BlocksDefList[curblocknumber] := curblockoffset;
    // --- Fill 512 samples arrays ----
    for i:=0 to NCW_SAMPLES-1 do
      for j:=0 to Header.Channels-1 do
      begin
        if (curblocknumber*512*Header.Channels+i*Header.Channels+j)<Length(data16) then
          temp16[j,i] := data16[curblocknumber*512*Header.Channels+i*Header.Channels+j]
        else
          temp16[j,i] := 0;
      end;


    // --- Consequential array handling  ----
    for j:=0 to Header.Channels-1 do
    begin
      DifArray16(temp16[j],temp16dif[j],max_,min_);   //--- Find differences --
      nbits := Max(MinBits(min_),MinBits(max_));   //--- Find miminal bits --

      FillBlockHeader(bHeader);
      bHeader.BaseValue := temp16[j,0];
      if nbits>=Header.Bits then
      begin
        bHeader.bits:=-Header.Bits;
        nbits := Header.Bits;
      end
      else
        bHeader.bits := nbits;

      blocksize := nbits*64;
      //ClearByteArray(tempb);

      if bHeader.bits<0 then
        TMyBits.Encode_16(NCW_SAMPLES,nbits,@temp16[j,0],@tempb[0])
      else
        TMyBits.Encode_16(NCW_SAMPLES,nbits,@temp16dif[j,0],@tempb[0]);
      ms.Write(bHeader,sizeof(bHeader));
      ms.Write(tempb[0],blocksize);
      curblockoffset := curblockoffset + sizeof(bHeader)+blocksize;
    end;
  end;
  BlocksDefList[Length(BlocksDefList)-1] := curblockoffset;
  Header.blocks_size := curblockoffset;
  Header.blocks_offset := sizeof(Header) + Length(BlocksDefList)*4;
end;

//==============================================================================
procedure TNCWParser.WriteNCW32(const WavHeader:TMyWAVHeader);
label ex;
var i,j:integer;
    bHeader:TBlockHeader;
    temp32:array [0..4,0..NCW_SAMPLES-1] of integer;
    temp32dif:array [0..4,0..NCW_SAMPLES-1] of integer;
    tempb:array of byte;
    nbits:integer;
    curblocknumber:integer;
    nblocks:integer;
    blocksize:integer;
    curblockoffset:integer;
    max_,min_:integer;
begin

  // --- Fill header ----
  with Header do
  begin
    CopyMemory(@Signature,
    @NCW_SIGNATURE1,sizeof(Signature));
    Channels := wavHeader.nChannels;
    Bits := wavHeader.wBitsPerSample;
    Samplerate := wavHeader.nSamplesPerSec;
    numSamples := wavHeader.numofpoints;
    FillChar(some_data,sizeof(some_data),0);
    block_def_offset := $78;
    nblocks := numSamples div 512 + 1;
    if (numSamples mod 512)<>0 then Inc(nblocks);
    blocks_offset := block_def_offset + DWORD(nblocks)*4;
  end;

  ms := TMemoryStream.Create;
  SetLength(BlocksDefList,nblocks);

  curblockoffset := 0;
  SetLength(tempb,Header.Bits*64*4);

  // --- Handling data by blocks ----
  for curblocknumber:=0 to nblocks-2 do
  begin
    BlocksDefList[curblocknumber] := curblockoffset;
    // --- Fill 512 samples arrays ----
    for i:=0 to NCW_SAMPLES-1 do
      for j:=0 to Header.Channels-1 do
      begin
        if (curblocknumber*512*Header.Channels+i*Header.Channels+j)<Length(datai) then
          temp32[j,i] := datai[curblocknumber*512*Header.Channels+i*Header.Channels+j]
        else
          temp32[j,i] := 0;
      end;


    // --- Consequential array handling  ----
    for j:=0 to Header.Channels-1 do
    begin
      DifArray32(temp32[j],temp32dif[j],max_,min_);   //--- Find differences --
      nbits := Max(MinBits(min_),MinBits(max_));   //--- Find miminal bits --

      FillBlockHeader(bHeader);
      bHeader.BaseValue := temp32[j,0];
      if nbits>=Header.Bits then
      begin
        bHeader.bits:=-Header.Bits;
        nbits := Header.Bits;
      end
      else
        bHeader.bits := nbits;

      blocksize := nbits*64;
      //ClearByteArray(tempb);

      if bHeader.bits<0 then
        TMyBits.Encode_32(NCW_SAMPLES,nbits,@temp32[j,0],@tempb[0])
      else
        TMyBits.Encode_32(NCW_SAMPLES,nbits,@temp32dif[j,0],@tempb[0]);
      ms.Write(bHeader,sizeof(bHeader));
      ms.Write(tempb[0],blocksize);
      curblockoffset := curblockoffset + sizeof(bHeader)+blocksize;
    end;
  end;
  BlocksDefList[Length(BlocksDefList)-1] := curblockoffset;
  Header.blocks_size := curblockoffset;
  Header.blocks_offset := sizeof(Header) + Length(BlocksDefList)*4;
end;

//==============================================================================
procedure TNCWParser.WriteNCW24(const WavHeader:TMyWAVHeader);
label ex;
var i,j,k:integer;
    bHeader:TBlockHeader;
    temp24:array [0..4,0..NCW_SAMPLES-1] of int24;
    temp24dif:array [0..4,0..NCW_SAMPLES-1] of int24;
    tempb:array of byte;
    nbits:integer;
    curblocknumber:integer;
    nblocks:integer;
    blocksize:integer;
    curblockoffset:integer;
    max_,min_:integer;
begin

  // --- Fill header ----
  with Header do
  begin
    CopyMemory(@Signature,
    @NCW_SIGNATURE1,sizeof(Signature));
    Channels := wavHeader.nChannels;
    Bits := wavHeader.wBitsPerSample;
    Samplerate := wavHeader.nSamplesPerSec;
    numSamples := wavHeader.numofpoints;
    FillChar(some_data,sizeof(some_data),0);
    block_def_offset := $78;
    nblocks := numSamples div 512 + 1;
    if (numSamples mod 512)<>0 then Inc(nblocks);
    blocks_offset := block_def_offset + DWORD(nblocks)*4;
  end;

  ms := TMemoryStream.Create;
  SetLength(BlocksDefList,nblocks);

  curblockoffset := 0;
  SetLength(tempb,Header.Bits*64*3);

  // --- Handling data by blocks ----
  for curblocknumber:=0 to nblocks-2 do
  begin
    BlocksDefList[curblocknumber] := curblockoffset;
    // --- Fill 512 samples arrays ----
    for i:=0 to NCW_SAMPLES-1 do
      for j:=0 to Header.Channels-1 do
      begin
        if (curblocknumber*512*Header.Channels+i*Header.Channels+j)<Length(data24) then
          temp24[j,i] := data24[curblocknumber*512*Header.Channels+i*Header.Channels+j]
        else
         for k:=0 to 2 do
           temp24[j,i][k] := 0;
      end;


    // --- Consequential array handling  ----
    for j:=0 to Header.Channels-1 do
    begin
      DifArray24(temp24[j],temp24dif[j],max_,min_);  //--- Find differences --
      nbits := Max(MinBits(min_),MinBits(max_));     //--- Find miminal bits --

      FillBlockHeader(bHeader);

      bHeader.BaseValue := temp24[j,0][0] or (temp24[j,0][1] shl 8) or (temp24[j,0][2] shl 16);
      if (temp24[j,0][2] and $80)<>0 then
        bHeader.BaseValue := integer(DWORD(bHeader.BaseValue) or $FF000000);

      if nbits>=Header.Bits then
      begin
        bHeader.bits:=-Header.Bits;
        nbits := Header.Bits;
      end
      else
        bHeader.bits := nbits;

      blocksize := nbits*64;
      //ClearByteArray(tempb);

      if bHeader.bits<0 then
        TMyBits.Encode_24(NCW_SAMPLES,nbits,@temp24[j,0],@tempb[0])
      else
        TMyBits.Encode_24(NCW_SAMPLES,nbits,@temp24dif[j,0],@tempb[0]);
      ms.Write(bHeader,sizeof(bHeader));
      ms.Write(tempb[0],blocksize);
      curblockoffset := curblockoffset + sizeof(bHeader)+blocksize;
    end;
  end;
  BlocksDefList[Length(BlocksDefList)-1] := curblockoffset;
  Header.blocks_size := curblockoffset;
  Header.blocks_offset := sizeof(Header) + Length(BlocksDefList)*4;
end;


end.
