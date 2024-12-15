# rand_r v2 第3章 例題コード集

## ビルド(miditoTcp)
Visual Studio 2022で.slnファイルを開き、コードを修正した後そのままビルド. (tested at windows 10 and 11)

## ビルド(hook.c)
```shell
gcc -fPIC -c -o hook.o hook.c 
gcc -shared -o hook.so hook.o -ldl
```

## miditoTcpデータ構造
|Byte No|Value|Explane|
|-|-|-|
|0|0xFF|STX|
|1|0xXX|Follow-up data length|
|2|0xF5|Port Specified Start Byte (Is this MIDI standard?)|
|3|0xXX|Port number|
|4|0xXX|Midi message data|
|...||MIDI data continues|
