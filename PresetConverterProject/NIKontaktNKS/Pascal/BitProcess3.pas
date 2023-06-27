unit BitProcess3;

interface

uses SysUtils,Classes, Windows, Math;

//const mask32:array [0..31] of DWORD = ();

const Zero24:array [0..2] of byte = (0,0,0);

type Int24 = packed array [0..2] of byte;
type Pint24 = ^Int24;
type rint24 = packed record
                b1,b2,b3:byte;
              end;
type print24 = ^rint24;
{type PInt24 = ^int24;
type Tint64Array = array of Int24;
type Pint64Array = ^Tint64array;}




type TMyBits = class
      class procedure FillIntegers(n:integer; bits:integer; data:pointer;
                start:integer; var ints:array of integer;
                relative:boolean);
      class procedure FillIntegersAbs(n:integer; bits:integer; data:pointer;
              start:integer; var ints:array of integer);
      class procedure FillBits(n:integer; bits:integer; data:pointer;
              const ints:array of integer);
      class procedure FillBitsAbs(n:integer; bits:integer; data:pointer;
              const ints:array of integer);

      class procedure Fill8(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
      class procedure Fill16(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
      class procedure Fill24(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
      class procedure Fill32(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);

      class procedure Encode_8(n:integer; bits:integer; source:pointer;
                dest:pointer);
      class procedure Encode_16(n:integer; bits:integer; source:pointer;
                dest:pointer);
      class procedure Encode_24(n:integer; bits:integer; source:pointer;
                dest:pointer);
      class procedure Encode_32(n:integer; bits:integer; source:pointer;
                dest:pointer);


    end;


function nbits32(n:integer):dword;
function ChangeIntSign(i:integer; sbit:integer):integer;
procedure EncodeL_16(n:integer; bits:integer; source:Pointer; dest:Pointer);


implementation


function nbits8(n:integer):BYTE;
var i:integer;
begin
  if n>8 then Result := $FF else
  if n=0 then Result :=0 else
  begin
    Result := 1;
    for i:=2 to n do
      Result := Result shl 1 + 1;
  end;
end;


function nbits16(n:integer):WORD;
var i:integer;
begin
  if n>16 then Result := $FFFF else
  if n=0 then Result :=0 else
  begin
    Result := 1;
    for i:=2 to n do
      Result := Result shl 1 + 1;
  end;
end;


function nbits32(n:integer):DWORD;
var i:integer;
begin
  if n>32 then Result := $FFFFFFFF else
  if n=0 then Result :=0 else
  begin
    Result := 1;
    for i:=2 to n do
      Result := Result shl 1 + 1;
  end;
end;


function nbits32old(n:integer):DWORD;
var i:integer;
begin
  if n>16 then Result := $FFFFFFFF else
  if n=0 then Result :=0 else
  begin
    Result := 1;
    for i:=2 to n do
      Result := Result shl 1 + 1;
  end;
end;

function ChangeIntSign(i:integer; sbit:integer):integer;
var DW:DWORD;
begin
  if i and (1 shl (sbit-1))<>0 then
  begin
    dw := DWORD(i);
    dw := dw or nbits32(32-sbit) shl sbit;
    Result := integer(dw);
  end
  else
    Result := i;
end;

//==============================================================================


procedure FillIntegers8(n:integer; data:pointer; var ints:array of integer; abs_:boolean);
var cur:integer;
    s:^shortint;
    start:integer;
begin
  s := data;
  if abs_ then start:=0 else start:=1;
  for cur:=start to n-1 do
  begin
    ints[cur] := s^;
    Inc(s);
  end;
  if not abs_ then ints[0]:=0;
end;


//===--------======--------======--------======--------======--------======-----
procedure FillIntegers16(n:integer; data:pointer; var ints:array of integer; abs_:boolean);
var cur:integer;
    s:^smallint;
    start:integer;
begin
  s := data;
  if abs_ then start:=0 else start:=1;
  for cur:=start to n-1 do
  begin
    ints[cur] := s^;
    Inc(s);
  end;
  if not abs_ then ints[0]:=0;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillIntegers24(n:integer; data:pointer; var ints:array of integer; abs_:boolean);
var t:integer;
    dw:DWORD;
    cur:integer;
    b:^byte;
    start:integer;
begin
  b := data;
  if abs_ then start:=0 else start:=1;
  for cur:=start to n-1 do
  begin
    dw:=b^;
    Inc(b);
    dw:=dw + b^ shl 8;
    Inc(b);
    dw:=dw + b^ shl 16;
    if (b^ and 128)=0 then
      t := dw
    else
      t := ChangeIntSign(dw,24);
    Inc(b);
    ints[cur]:=t;
  end;
  if not abs_ then ints[0]:=0;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillIntegers32(n:integer; data:pointer; var ints:array of integer; abs_:boolean);
var ip:^integer;
    cur:integer;
    start:integer;
begin
  ip := data;
  if abs_ then start:=0 else start:=1;
  for cur:=start to n-1 do
  begin
    ints[cur] := ip^;
    Inc(ip);
  end;
  if not abs_ then ints[0]:=0;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillIntegersL8(n:integer; bits:integer; data:pointer; var ints:array of integer);
var j:integer;
    t:integer;
    dw:DWORD;
    cur:integer;
    b:^byte;
    tb:byte;
    bitstotal:integer;
begin
  b := data;
  bitstotal:=0;
  tb := b^;
  for cur:=1 to n-1 do
  begin
    dw := 0;
    for j:=0 to bits-1 do
    begin
      dw := dw + (tb and 1)shl j;
      tb := tb shr 1;
      Inc(bitstotal);
      if bitstotal=8 then
      begin
        Inc(b);
        tb := b^;
        bitstotal:=0;
      end;
    end;
    t := ChangeIntSign(dw,bits);
    ints[cur]:=t;
  end;
  ints[0]:=0;
end;




//==============================================================================

procedure FillBits8(n:integer; const ints:array of integer; data:pointer; abs_:boolean);
var cur:integer;
    s:^shortint;
    start:integer;
begin
  s := data;
  start:=0;
  for cur:=start to n-1 do
  begin
    s^ := ints[cur];
    Inc(s);
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillBits16(n:integer; const ints:array of integer; data:pointer; abs_:boolean);
var cur:integer;
    s:^smallint;
    start:integer;
begin
  s := data;
  start:=0;
  for cur:=start to n-1 do
  begin
    s^ := ints[cur];
    Inc(s);
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillBits24(n:integer; const ints:array of integer; data:pointer; abs_:boolean);
var cur:integer;
    b:^byte;
    t:integer;
    start:integer;
begin
  b := data;
  start:=0;
  for cur:=start to n-1 do
  begin
    t := ints[cur];
    b^:= t and $FF;
    t := t shr 8;
    Inc(b);
    b^:= t and $FF;
    t := t shr 8;
    Inc(b);
    b^:= t and $FF;
    if ints[cur]<0 then b^:=b^ or $80;
    Inc(b);
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillBits32(n:integer; const ints:array of integer; data:pointer; abs_:boolean);
var cur:integer;
    ip:^integer;
    start:integer;
begin
  ip := data;
  start:=0;
  for cur:=start to n-1 do
  begin
    ip^ := ints[cur];
    Inc(ip);
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure FillBitsL8(n:integer; bits:integer; const ints:array of integer; data:pointer);
var j:integer;
    dw:DWORD;
    cur:integer;
    b:^byte;
    tb:byte;
    bitswritten:integer;
begin
  b := data;
  bitswritten:=0;
  tb := 0;
  for cur:=0 to n-1 do
  begin
    dw := DWORD(ints[cur]);
    for j:=0 to bits-1 do
    begin
      tb := tb + (dw and 1)shl bitswritten;
      dw := dw shr 1;
      Inc(bitswritten);
      if bitswritten=8 then
      begin
        b^ := tb;
        tb := 0;
        Inc(b);
        bitswritten:=0;
      end;
    end;
  end;
end;




//===--------======--------======--------======--------======--------======-----
procedure Encode8_8(n:integer; source:Pointer; dest:pointer);
begin
  Move(source^,dest^,n);
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode8_16(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^smallint;
    sd:^shortint;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^;
    Inc(ss);
    Inc(sd);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode8_24(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^int24;
    sd:^shortint;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^[0];
    Inc(ss);
    Inc(sd);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode8_32(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^integer;
    sd:^shortint;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^;
    Inc(ss);
    Inc(sd);
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure Encode16_16(n:integer; source:Pointer; dest:Pointer);
begin
  Move(source^,dest^,n*2);
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode16_24(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^byte;
    sd:^byte;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^;
    Inc(ss); Inc(sd);
    sd^ := ss^;
    Inc(ss); Inc(sd);
    Inc(ss);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode16_32(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^integer;
    sd:^smallint;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^;
    Inc(ss);
    Inc(sd);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode24_24(n:integer; source:Pointer; dest:Pointer);
begin
  Move(source^,dest^,n*3);
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode24_32(n:integer; source:Pointer; dest:pointer);
var i:integer;
    ss:^byte;
    sd:^byte;
begin
  ss:=source;
  sd:=dest;
  for i:=0 to n-1 do
  begin
    sd^ := ss^;
    Inc(ss); Inc(sd);
    sd^ := ss^;
    Inc(ss); Inc(sd);
    sd^ := ss^;
    Inc(ss); Inc(sd);
    Inc(ss);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Encode32_32(n:integer; source:Pointer; dest:pointer);
begin
  Move(source^,dest^,n*4);
end;

//===--------======--------======--------======--------======--------======-----
procedure EncodeL_8(n:integer; bits:integer; source:Pointer; dest:Pointer);
var db:^byte;
    cur:integer;
    b:^byte;
    tb:byte;
    bitswritten:integer;
    bitsleft:integer;    
begin
  b := dest;
  db := source;
  b^:=0;
  bitsleft := 8;

  for cur:=0 to n-1 do
  begin
    bitswritten := 0;
    while bitswritten<bits do
    begin
      if (bits-bitswritten)<=bitsleft then
      begin
        tb := (db^ and (($FF shr (8-(bits-bitswritten)))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitsleft := bitsleft - (bits-bitswritten);
        bitswritten := bits;
      end
      else
      begin
        tb := (db^ and (($FF shr (8-bitsleft))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitswritten := bitswritten+bitsleft;
        bitsleft := 0;
      end;
      if bitsleft=0 then
      begin
        Inc(b);
        b^:=0;
        bitsleft:=8;
      end;
    end;
    Inc(db);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure EncodeL_16(n:integer; bits:integer; source:Pointer; dest:Pointer);
var dw:^word;
    cur:integer;
    b:^byte;
    tb:byte;
    bitswritten:integer;
    bitsleft:integer;
begin
  b := dest;
  dw := source;
  b^:=0;
  bitsleft := 8;

  for cur:=0 to n-1 do
  begin
    bitswritten := 0;
    while bitswritten<bits do
    begin
      if (bits-bitswritten)<=bitsleft then
      begin
        tb := (dw^ and (($FF shr (8-(bits-bitswritten)))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitsleft := bitsleft - (bits-bitswritten);
        bitswritten := bits;
      end
      else
      begin
        tb := (dw^ and (($FF shr (8-bitsleft))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitswritten := bitswritten+bitsleft;
        bitsleft := 0;
      end;
      if bitsleft=0 then
      begin
        Inc(b);
        b^:=0;
        bitsleft:=8;
      end;
    end;
    Inc(dw);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure EncodeL_24(n:integer; bits:integer; source:Pointer; dest:Pointer);
var d24:^int24;
    dw:DWORD;
    cur:integer;
    b:^byte;
    tb:byte;
    bitswritten:integer;
    bitsleft:integer;
begin
  b := dest;
  d24 := source;
  b^:=0;
  bitsleft := 8;

  for cur:=0 to n-1 do
  begin
    dw := d24^[0] or (d24^[1] shl 8) or (d24^[2] shl 16);
    bitswritten := 0;
    while bitswritten<bits do
    begin
      if (bits-bitswritten)<=bitsleft then
      begin
        tb := (dw and (($FF shr (8-(bits-bitswritten)))));
        dw := dw shr (bits-bitswritten);
        b^ := b^ or (tb shl (8-bitsleft));
        bitsleft := bitsleft - (bits-bitswritten);
        bitswritten := bits;
      end
      else
      begin
        tb := (dw and (($FF shr (8-bitsleft))));
        dw := dw shr bitsleft;
        b^ := b^ or (tb shl (8-bitsleft));
        bitswritten := bitswritten+bitsleft;
        bitsleft := 0;
      end;
      if bitsleft=0 then
      begin
        Inc(b);
        b^:=0;
        bitsleft:=8;
      end;
    end;
    Inc(d24);
  end;



end;
//===--------======--------======--------======--------======--------======-----
procedure EncodeL_32(n:integer; bits:integer; source:Pointer; dest:Pointer);
var dw:^dword;
    cur:integer;
    b:^byte;
    tb:byte;
    bitswritten:integer;
    bitsleft:integer;
begin
  b := dest;
  dw := source;
  b^:=0;
  bitsleft := 8;

  for cur:=0 to n-1 do
  begin
    bitswritten := 0;
    while bitswritten<bits do
    begin
      if (bits-bitswritten)<=bitsleft then
      begin
        tb := (dw^ and (($FF shr (8-(bits-bitswritten)))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitsleft := bitsleft - (bits-bitswritten);
        bitswritten := bits;
      end
      else
      begin
        tb := (dw^ and (($FF shr (8-bitsleft))shl bitswritten))shr bitswritten;
        b^ := b^ or (tb shl (8-bitsleft));
        bitswritten := bitswritten+bitsleft;
        bitsleft := 0;
      end;
      if bitsleft=0 then
      begin
        Inc(b);
        b^:=0;
        bitsleft:=8;
      end;
    end;
    Inc(dw);
  end;
end;






//===--------======--------======--------======--------======--------======-----
procedure Fill16_8rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:pshortint;
    sd:psmallint;
begin
  ss  := source;
  sd  := dest;
  sd^ := base_value;
  for i:=1 to n-1 do
  begin
    Inc(sd);
    sd^ := ss^ + psmallint(DWORD(sd)-2)^;
    Inc(ss);
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill24_8rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:pshortint;
    sd:^int24;
    it1,it2:integer;
begin
  ss  := source;
  sd  := dest;
  it1 := base_value;
  sd^[0] := it1 and $FF;
  sd^[1] := (it1 shr 8)and $FF;
  sd^[2] := (it1 shr 16)and $FF;

  for i:=1 to n-1 do
  begin
    Inc(sd);
    it2 := ss^;
    it1 := it1+it2;
    sd^[0] := it1 and $FF;
    sd^[1] := (it1 shr 8)and $FF;
    sd^[2] := (it1 shr 16)and $FF;
    Inc(ss);    
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill24_16rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:psmallint;
    sd:^int24;
    it1,it2:integer;
begin
  ss  := source;
  sd  := dest;
  it1 := base_value;
  sd^[0] := it1 and $FF;
  sd^[1] := (it1 shr 8)and $FF;
  sd^[2] := (it1 shr 16)and $FF;

  for i:=1 to n-1 do
  begin
    Inc(sd);
    it2 := ss^;
    it1 := it1+it2;
    sd^[0] := it1 and $FF;
    sd^[1] := (it1 shr 8)and $FF;
    sd^[2] := (it1 shr 16)and $FF;
    Inc(ss);    
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill32_8rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:pshortint;
    sd:pinteger;
begin
  ss  := source;
  sd  := dest;
  sd^ := base_value;

  for i:=1 to n-1 do
  begin
    Inc(sd);
    sd^ := ss^ + pinteger(DWORD(sd)-4)^;
    Inc(ss);    
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill32_16rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:psmallint;
    sd:pinteger;
begin
  ss  := source;
  sd  := dest;
  sd^ := base_value;

  for i:=1 to n-1 do
  begin
    Inc(sd);
    sd^ := ss^ + pinteger(DWORD(sd)-4)^;
    Inc(ss);    
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill32_24rel(n:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    ss:pint24;
    sd:pinteger;
    it:integer;
begin
  ss  := source;
  sd  := dest;
  sd^ := base_value;

  for i:=1 to n-1 do
  begin               
    it := (ss^[0]) or (ss^[1] shl 8) or (ss^[2] shl 16);
    if (ss^[2] and $80)<>0 then DWORD(it) := DWORD(it) or $FF000000;
    it := it + sd^;
    Inc(sd);
    sd^ := it;
    Inc(ss);    
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill8abs(n:integer; source:pointer; dest:pointer);
begin
  Move(source^,dest^,n);
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill16abs(n:integer; source:pointer; dest:pointer);
begin
  Move(source^,dest^,n*2);
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill24abs(n:integer; source:pointer; dest:pointer);
begin
  Move(source^,dest^,n*3);
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill32abs(n:integer; source:pointer; dest:pointer);
begin
  Move(source^,dest^,n*4);
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill8_bits(n:integer; bits:integer; source:pointer; dest:pointer; base_value:integer);
var i,j:integer;
    db:shortint;
    sb:pshortint;
    b:^byte;
    tb:byte;
    bitstotal:integer;
begin
  b := source;
  sb := dest;
  sb^ := base_value;
  bitstotal:=0;
  tb := b^;
  for i:=1 to n-1 do
  begin
    db := 0;
    for j:=0 to bits-1 do
    begin
      db := db or ((tb and 1)shl j);
      tb := tb shr 1;
      Inc(bitstotal);
      if bitstotal=8 then
      begin
        Inc(b);
        tb := b^;
        bitstotal:=0;
      end;
    end;
    if db and (1 shl (bits-1))<>0 then
      db := db or ($FF shl bits);
    db := db+sb^;
    Inc(sb);
    sb^ := db;
  end;
end;
//===--------======--------======--------======--------======--------======-----
procedure Fill16_bits(n:integer; bits:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    dw:smallint;
    ss:psmallint;
    b:^byte;
    tb:byte;
    bitsleft:integer;
    bitsneeded:integer;
begin
  b := source;
  ss := dest;
  ss^ := base_value;
  tb := b^;
  bitsleft:=8;

  for i:=1 to n-1 do
  begin
    dw := 0;
    bitsneeded := bits;

    while bitsneeded>0 do
    begin
      if bitsneeded>=bitsleft then
      begin
        dw := dw or ((tb and ($FF shr (8-bitsleft))) shl (bits-bitsneeded));
        Inc(b);
        tb := b^;
        bitsneeded := bitsneeded-bitsleft;
        bitsleft := 8;
      end
      else
      begin
        dw := dw or ((tb and ($FF shr (8-bitsneeded))) shl (bits-bitsneeded));
        tb := tb shr bitsneeded;
        bitsleft := bitsleft-bitsneeded;
        bitsneeded := 0;
      end;
    end;
    if (dw and (1 shl (bits-1)))<> 0 then
      dw := dw or ($FFFF shl bits);
    dw := ss^+dw;
    Inc(ss);
    ss^ := dw;
  end;
end;

//===--------======--------======--------======--------======--------======-----
procedure Fill24_bits(n:integer; bits:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    dd:integer;
    si:pint24;
    b:^byte;
    tb:byte;
    ti:integer;
    bitsleft:integer;
    bitsneeded:integer;
begin
  b := source;
  si := dest;
  ti := base_value;
  si^[0]:= ti and $FF;
  si^[1]:=(ti shr 8)and $FF;
  si^[2]:=(ti shr 16)and $FF;
  tb := b^;
  bitsleft := 8;

  for i:=1 to n-1 do
  begin
    dd := 0;
    bitsneeded := bits;

    while bitsneeded>0 do
    begin
      if bitsneeded>=bitsleft then
      begin
        dd := dd or ((tb and ($FF shr (8-bitsleft))) shl (bits-bitsneeded));
        Inc(b);
        tb := b^;
        bitsneeded := bitsneeded-bitsleft;
        bitsleft := 8;
      end
      else
      begin
        dd := dd or ((tb and ($FF shr (8-bitsneeded))) shl (bits-bitsneeded));
        tb := tb shr bitsneeded;
        bitsleft := bitsleft-bitsneeded;
        bitsneeded := 0;
      end;
    end;
    if dd and (1 shl (bits-1))<>0 then
      dd := DWORD(dd) or ($FFFFFFFF shl bits);
    ti := ti+dd;
    Inc(si);
    si^[0]:= ti and $FF;
    si^[1]:=(ti shr 8)and $FF;
    si^[2]:=(ti shr 16)and $FF;
  end;
end;


//===--------======--------======--------======--------======--------======-----
procedure Fill32_bits(n:integer; bits:integer; source:pointer; dest:pointer; base_value:integer);
var i:integer;
    dd:integer;
    si:pinteger;
    b:^byte;
    tb:byte;
    bitsneeded:integer;
    bitsleft:integer;
begin
  b := source;
  si := dest;
  si^ := base_value;

  bitsleft := 8;

  tb := b^;
  for i:=1 to n-1 do
  begin
    dd := 0;
    bitsneeded := bits;

    while bitsneeded>0 do
    begin
      if bitsneeded>=bitsleft then
      begin
        dd := dd or ((tb and ($FF shr (8-bitsleft))) shl (bits-bitsneeded));
        Inc(b);
        tb := b^;
        bitsneeded := bitsneeded-bitsleft;
        bitsleft := 8;
      end
      else
      begin
        dd := dd or ((tb and ($FF shr (8-bitsneeded))) shl (bits-bitsneeded));
        tb := tb shr bitsneeded;
        bitsleft := bitsleft-bitsneeded;
        bitsneeded := 0;
      end;
    end;
      if dd and (1 shl (bits-1))<>0 then
      dd := DWORD(dd) or ($FFFFFFFF shl bits);
    dd := si^+dd;
    Inc(si);
    si^ := dd;
  end;
end;


class procedure TMyBits.FillIntegers(n:integer; bits:integer; data:pointer;
              start:integer; var ints:array of integer;
              relative:boolean);
var i:integer;
begin
  if bits=8 then FillIntegers8(n,data,ints,false) else
  if bits=16 then FillIntegers16(n,data,ints,false) else
  if bits=24 then FillIntegers24(n,data,ints,false) else
  if bits=32 then FillIntegers32(n,data,ints,false) else
                  FillIntegersL8(n,bits,data,ints);
  if relative then
  begin
    ints[0]:=start+ints[0];
    for i:=1 to n-1 do
      ints[i]:=ints[i-1]+ints[i];
  end;
end;



class procedure TMyBits.FillIntegersAbs(n:integer; bits:integer; data:pointer;
              start:integer; var ints:array of integer);
begin
  if bits=8 then FillIntegers8(n,data,ints,true) else
  if bits=16 then FillIntegers16(n,data,ints,true) else
  if bits=24 then FillIntegers24(n,data,ints,true) else
  if bits=32 then FillIntegers32(n,data,ints,true);
end;




class procedure TMyBits.FillBits(n:integer; bits:integer; data:pointer;
              const ints:array of integer);
begin
  if bits=8 then FillBits8(n,ints,data,false) else
  if bits=16 then FillBits16(n,ints,data,false) else
  if bits=24 then FillBits24(n,ints,data,false) else
  if bits=32 then FillBits32(n,ints,data,false) else
                  FillBitsL8(n,bits,ints,data);
end;




class procedure TMyBits.FillBitsAbs(n:integer; bits:integer; data:pointer;
              const ints:array of integer);
begin
  if bits=8 then FillBits8(n,ints,data,true) else
  if bits=16 then FillBits16(n,ints,data,true) else
  if bits=24 then FillBits24(n,ints,data,true) else
  if bits=32 then FillBits32(n,ints,data,true);
end;



class procedure TMyBits.Fill8(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
begin
  if relative then
    Fill8_bits(n,bits,source,dest,base_value)
  else
    Fill8abs(n,source,dest);
end;


class procedure TMyBits.Fill16(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
begin
  if relative then
    if bits=8 then
      Fill16_8rel(n,source,dest,base_value)
    else
      Fill16_bits(n,bits,source,dest,base_value)
  else
    Fill16abs(n,source,dest);
end;


class procedure TMyBits.Fill24(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
begin
  if relative then
    case bits of
    8: Fill24_8rel(n,source,dest,base_value);
    16:Fill24_16rel(n,source,dest,base_value);
    else Fill24_bits(n,bits,source,dest,base_value);
    end
  else
    Fill24abs(n,source,dest);
end;


class procedure TMyBits.Fill32(n:integer; bits:integer; source:pointer;
                base_value:integer; dest:pointer;  relative:boolean);
begin
  if relative then
    case bits of
    8: Fill32_8rel(n,source,dest,base_value);
    16:Fill32_16rel(n,source,dest,base_value);
    24:Fill32_24rel(n,source,dest,base_value);
    else Fill32_bits(n,bits,source,dest,base_value);
    end
  else
    Fill32abs(n,source,dest);
end;

class procedure TMyBits.Encode_8(n:integer; bits:integer; source:Pointer;
                                    dest:Pointer);
begin
  if bits=8 then Encode8_8(n,source,dest) else
    EncodeL_8(n,bits,source,dest);
end;

class procedure TMyBits.Encode_16(n:integer; bits:integer; source:Pointer;
                                    dest:Pointer);
begin
  if bits=8 then Encode8_16(n,source,dest) else
  if bits=16 then Encode16_16(n,source,dest) else
    EncodeL_16(n,bits,source,dest);
end;

class procedure TMyBits.Encode_24(n:integer; bits:integer; source:Pointer;
                                    dest:Pointer);
begin
  if bits=8 then Encode8_24(n,source,dest) else
  if bits=16 then Encode16_24(n,source,dest) else
  if bits=24 then Encode24_24(n,source,dest) else  
    EncodeL_24(n,bits,source,dest);
end;

class procedure TMyBits.Encode_32(n:integer; bits:integer; source:Pointer;
                                    dest:Pointer);
begin
  if bits=8 then Encode8_32(n,source,dest) else
  if bits=16 then Encode16_32(n,source,dest) else
  if bits=24 then Encode24_32(n,source,dest) else
  if bits=32 then Encode32_32(n,source,dest) else  
    EncodeL_32(n,bits,source,dest);
end;



end.
