# Disassembled Midi file to Arduino header file converter
Download and install MIDI File Disassembler/Assembler from here:<br/>
http://midi.teragonaudio.com/progs/software.htm#dsm<br/>
Export .txt file using default settings.<br/>
Place exported files in the same folder as this application.<br/>
Run this application and follow the instructions to convert the disassembled midi file.<br/>
<br/>
The converted header file can then be used together with the Arduino project found here:<br/>
https://github.com/birkb85/BirKen_Music_Player_Plus<br/>
<br/>

## Publish executable
Publish the project using a folder profile with settings like theese:<br/>
<br/>
```
<?xml version="1.0" encoding="utf-8"?>
<!--
https://go.microsoft.com/fwlink/?LinkID=208121.
-->
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>bin\Release\net7.0\publish\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <_TargetId>Folder</_TargetId>
    <TargetFramework>net7.0</TargetFramework>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
</Project>
```