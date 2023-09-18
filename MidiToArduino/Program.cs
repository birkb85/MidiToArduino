// Source available on:
// https://github.com/birkb85/MidiToArduino

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

Console.WriteLine("-------------------------------------------------------");
Console.WriteLine("Disassembled Midi file to Arduino header file converter");
Console.WriteLine("-------------------------------------------------------");
Console.WriteLine("Download and install MIDI File Disassembler/Assembler from here:");
Console.WriteLine("http://midi.teragonaudio.com/progs/software.htm#dsm");
Console.WriteLine("Export .txt file using default settings.");
Console.WriteLine("Place exported files in the same folder as this application.");
Console.WriteLine("Then run this application again.");

string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
string? strWorkPath = Path.GetDirectoryName(strExeFilePath);
if (strWorkPath == null)
{
    Console.WriteLine("Error: Application path not available.");
    Console.ReadKey();
    return;
}

Console.WriteLine("");
Console.WriteLine("List of files in this folder:");
string[] files = Directory.GetFiles(strWorkPath, "*.txt");
for (int i = 0; i < files.Length; i++)
{
    string fileName = Path.GetFileName(files[i]);
    Console.WriteLine($"{i}: {fileName}");
}

Console.WriteLine("");
Console.WriteLine("Input file number and press enter.");
string? numberString = Console.ReadLine();
bool success = int.TryParse(numberString, out int number);
if (!success || number < 0 || number >= files.Length)
{
    Console.WriteLine("Error: Number not available.");
    Console.ReadKey();
    return;
}

Console.WriteLine("");
Console.WriteLine("Processing...");

string file = files[number];
string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
string exportedFileName = fileNameWithoutExtension;
exportedFileName = exportedFileName.Replace(" ", "_");
exportedFileName = exportedFileName.Replace("-", "_");
exportedFileName = Regex.Replace(exportedFileName, "[^0-9A-Za-z_]", "");
string exportedFileNameWithoutExtension = exportedFileName;
exportedFileName = exportedFileName + ".h";
string exportedFilePath = $"{strWorkPath}\\{exportedFileName}";

bool firstLine = true;

int divisionFull = 0;
int currentTrack = -1;
int currentMeasure = 0;
int currentBeat = 0;
int currentDivision = 0;
Dictionary<string, string> timeSigs = new Dictionary<string, string>();
int currentTimeSigPart1 = 0;
int currentTimeSigPart2 = 0;
Dictionary<string, int> tempos = new Dictionary<string, int>();
int currentTempo = 0;

int lastOnNoteMeasure = 1;
int lastOnNoteBeat = 1;
int lastOnNoteDivision = 0;
int lastOnNoteTimeSigPart1 = 0;
int lastOnNoteTimeSigPart2 = 0;
int lastOnNoteTempo = 0;
int lastOffNoteMeasure = 1;
int lastOffNoteBeat = 1;
int lastOffNoteDivision = 0;
int lastOffNoteTimeSigPart1 = 0;
int lastOffNoteTimeSigPart2 = 0;
int lastOffNoteTempo = 0;

double durationRemainder = 0;

List<string> notes1 = new List<string>();
List<string> notes2 = new List<string>();
List<int> durations1 = new List<int>();
List<int> durations2 = new List<int>();

const string searchDivisionFull = "| Division=";
const string searchTrack = "Track #";
const string searchTrackEnd = "|End of track";
const string searchTimeSig = "|Time Sig";
const string searchTempo = "|Tempo";
const string searchTempoMicros = "| micros\\quarter=";
const string searchOnNote = "|On Note";
const string searchPitch = "| pitch=";
const string searchOffNote = "|Off Note";
foreach (string line in File.ReadLines(file))
{
    // Get Division Full
    if (firstLine)
    {
        firstLine = false;
        if (line.Contains(searchDivisionFull))
        {
            int divisionFullPos = line.IndexOf(searchDivisionFull) + searchDivisionFull.Length;
            string divisionFullStr = line.Substring(divisionFullPos);
            success = int.TryParse(divisionFullStr, out divisionFull);
            if (!success)
            {
                Console.WriteLine($"Error: Division not legal: {divisionFullStr}");
                Console.ReadKey();
                return;
            }
            Console.WriteLine($"Division: {divisionFull} per beat");
        }
    }

    // Get Track
    if (line.StartsWith(searchTrack))
    {
        string trackStr = line.Substring(searchTrack.Length).Replace("*", "").Trim();
        success = int.TryParse(trackStr, out currentTrack);
        if (!success)
        {
            Console.WriteLine($"Error: Track not legal: {trackStr}");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"");
        Console.WriteLine($"---- Track: {currentTrack} ----");
    }

    // Reset track related stuff at end of track
    if (line.Contains(searchTrackEnd))
    {
        currentMeasure = 0;
        currentBeat = 0;
        currentDivision = 0;

        lastOnNoteMeasure = 1;
        lastOnNoteBeat = 1;
        lastOnNoteDivision = 0;
        lastOnNoteTimeSigPart1 = 0;
        lastOnNoteTimeSigPart2 = 0;
        lastOnNoteTempo = 0;
        lastOffNoteMeasure = 1;
        lastOffNoteBeat = 1;
        lastOffNoteDivision = 0;
        lastOffNoteTimeSigPart1 = 0;
        lastOffNoteTimeSigPart2 = 0;
        lastOffNoteTempo = 0;

        if (durationRemainder != 0)
        {
            Console.WriteLine($"Duration remainder: {durationRemainder}");
            durationRemainder = 0;
        }
    }

    // Get time
    if (currentTrack >= 0 && line.Contains('|'))
    {
        int timeEndPos = line.IndexOf('|');
        string timeFull = line.Substring(0, timeEndPos).Trim();
        if (!string.IsNullOrEmpty(timeFull))
        {
            //Console.WriteLine($"Time Full: #{timeFull}#");

            string[] parts = timeFull.Split(":");

            // Get Measure
            if (parts.Length == 3)
            {
                success = int.TryParse(parts[0].Trim(), out currentMeasure);
                if (!success)
                {
                    Console.WriteLine($"Error: Measure not legal: {parts[0].Trim()}");
                    Console.ReadKey();
                    return;
                }
                //Console.WriteLine($"Measure: {measure}");
            }

            // Get Beat
            if (parts.Length >= 2)
            {
                success = int.TryParse(parts[parts.Length - 2].Trim(), out currentBeat);
                if (!success)
                {
                    Console.WriteLine($"Error: Beat not legal: {parts[parts.Length - 2].Trim()}");
                    Console.ReadKey();
                    return;
                }
                //Console.WriteLine($"Beat: {beat}");
            }

            // Get Division
            if (parts.Length >= 1)
            {
                success = int.TryParse(parts[parts.Length - 1].Trim(), out currentDivision);
                if (!success)
                {
                    Console.WriteLine($"Error: Division not legal: {parts[parts.Length - 1].Trim()}");
                    Console.ReadKey();
                    return;
                }
                //Console.WriteLine($"Division: {division}");
            }


        }
    }

    if (currentMeasure > 0)
    {
        // Get configuration
        if (currentTrack == 0)
        {
            // Get Time Sig
            if (line.Contains(searchTimeSig))
            {
                int timeSigPos = line.IndexOf(searchTimeSig);
                int startPos = line.IndexOf('|', timeSigPos + 1);
                int endPos = line.IndexOf('|', startPos + 1);
                string timeSigStr = line.Substring(startPos + 1, endPos - startPos - 1).Trim();
                //Console.WriteLine($"Time Sig Full: #{timeSigStr}#");

                string time = $"{currentMeasure}:{currentBeat}:{currentDivision}";
                timeSigs.Add(time, timeSigStr);
                Console.WriteLine($"Time: {time}, Time Sig: {timeSigStr}");
            }

            // Get Tempo
            if (line.Contains(searchTempo) && line.Contains(searchTempoMicros))
            {
                int microsPos = line.IndexOf(searchTempoMicros);
                string microsStr = line.Substring(microsPos + searchTempoMicros.Length);
                success = int.TryParse(microsStr, out int micros);
                if (!success)
                {
                    Console.WriteLine($"Error: Micros not legal: {microsStr}");
                    Console.ReadKey();
                    return;
                }

                string time = $"{currentMeasure}:{currentBeat}:{currentDivision}";
                int millis = micros / 1000;
                tempos.Add(time, millis);
                Console.WriteLine($"Time: {time}, Tempo: {millis} millis per quarter");
            }
        }
        else if (currentTrack > 0)
        {
            if (divisionFull == 0)
            {
                Console.WriteLine($"Error: No Division found");
                Console.ReadKey();
                return;
            }

            if (timeSigs.Count == 0)
            {
                Console.WriteLine($"Error: No Time Sigs found");
                Console.ReadKey();
                return;
            }

            if (tempos.Count == 0)
            {
                Console.WriteLine($"Error: No Tempos found");
                Console.ReadKey();
                return;
            }

            string time = $"{currentMeasure}:{currentBeat}:{currentDivision}";

            if (timeSigs.ContainsKey(time))
            {
                string[] parts = timeSigs[time].Split('/');
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Error: Time Sig not legal: {timeSigs[time]}");
                    Console.ReadKey();
                    return;
                }

                success = int.TryParse(parts[0], out int part1);
                if (!success)
                {
                    Console.WriteLine($"Error: Time Sig part not legal: {parts[0]}");
                    Console.ReadKey();
                    return;
                }

                success = int.TryParse(parts[1], out int part2);
                if (!success)
                {
                    Console.WriteLine($"Error: Time Sig part not legal: {parts[1]}");
                    Console.ReadKey();
                    return;
                }

                currentTimeSigPart1 = part1;
                currentTimeSigPart2 = part2;

                if (lastOffNoteTimeSigPart1 == 0 && lastOffNoteTimeSigPart2 == 0)
                {
                    lastOffNoteTimeSigPart1 = part1;
                    lastOffNoteTimeSigPart2 = part2;
                }
            }

            if (tempos.ContainsKey(time))
            {
                currentTempo = tempos[time];

                if (lastOffNoteTempo == 0)
                {
                    lastOffNoteTempo = tempos[time];
                }
            }

            if (line.Contains(searchOnNote))
            {
                // Pause
                if (lastOffNoteMeasure != currentMeasure ||
                    lastOffNoteBeat != currentBeat ||
                    lastOffNoteDivision != currentDivision)
                {
                    // Time passed
                    int measurePassed = currentMeasure - lastOffNoteMeasure;

                    int beatPassed;
                    if (currentBeat >= lastOffNoteBeat)
                    {
                        beatPassed = currentBeat - lastOffNoteBeat;
                    }
                    else
                    {
                        beatPassed = lastOffNoteTimeSigPart1 + currentBeat - lastOffNoteBeat;
                        measurePassed--;
                    }

                    int divisionPassed;
                    if (currentDivision >= lastOffNoteDivision)
                    {
                        divisionPassed = currentDivision - lastOffNoteDivision;
                    }
                    else
                    {
                        divisionPassed = divisionFull + currentDivision - lastOffNoteDivision;
                        beatPassed--;
                    }

                    // Calculate duration
                    double totalDivision =
                        ((double)divisionPassed + 
                        (double)beatPassed * (double)divisionFull + 
                        (double)measurePassed * (double)divisionFull * 4.0) * ((double)lastOffNoteTimeSigPart1 / (double)lastOffNoteTimeSigPart2);
                    double millis = (totalDivision / (double)divisionFull) * (double)lastOffNoteTempo;
                    int duration = (int)millis;
                    // Make up for unprecise duration.
                    durationRemainder += millis % 1.0;
                    if (durationRemainder >= 1.0)
                    {
                        Console.WriteLine($"Duration remainder: {durationRemainder}");
                        duration++;
                        durationRemainder--;
                    }

                    Console.WriteLine($"Pause: Time passed: {measurePassed}:{beatPassed}:{divisionPassed}, Duration: {duration}");

                    switch (currentTrack)
                    {
                        case 1:
                            notes1.Add("0");
                            durations1.Add(duration);
                            break;

                        case 2:
                            notes2.Add("0");
                            durations2.Add(duration);
                            break;
                    }
                }

                // Note
                int pitchPos = line.IndexOf(searchPitch) + searchPitch.Length;
                string pitch = line.Substring(pitchPos, 3).Replace(" ", "").Replace('#', 'S').ToUpper();
                string note = $"NOTE_{pitch}";

                string timeSig = currentTimeSigPart1 + "/" + currentTimeSigPart2;
                Console.WriteLine($"Time: {time}, Time Sig: {timeSig}, Tempo {currentTempo}, On Note: {note}");

                lastOnNoteMeasure = currentMeasure;
                lastOnNoteBeat = currentBeat;
                lastOnNoteDivision = currentDivision;
                lastOnNoteTimeSigPart1 = currentTimeSigPart1;
                lastOnNoteTimeSigPart2 = currentTimeSigPart2;
                lastOnNoteTempo = currentTempo;
            }
            else if (line.Contains(searchOffNote))
            {
                // Time passed
                int measurePassed = currentMeasure - lastOnNoteMeasure;

                int beatPassed;
                if (currentBeat >= lastOnNoteBeat)
                {
                    beatPassed = currentBeat - lastOnNoteBeat;
                }
                else
                {
                    beatPassed = lastOnNoteTimeSigPart1 + currentBeat - lastOnNoteBeat;
                    measurePassed--;
                }

                int divisionPassed;
                if (currentDivision >= lastOnNoteDivision)
                {
                    divisionPassed = currentDivision - lastOnNoteDivision;
                }
                else
                {
                    divisionPassed = divisionFull + currentDivision - lastOnNoteDivision;
                    beatPassed--;
                }

                // Calculate duration
                double totalDivision = 
                    ((double)divisionPassed +
                    (double)beatPassed * (double)divisionFull + 
                    (double)measurePassed * (double)divisionFull * 4.0) * ((double)lastOnNoteTimeSigPart1 / (double)lastOnNoteTimeSigPart2);
                double millis = (int)((totalDivision / (double)divisionFull) * (double)lastOnNoteTempo);
                int duration = (int)millis;
                // Make up for unprecise duration.
                durationRemainder += millis % 1.0;
                if (durationRemainder >= 1.0)
                {
                    Console.WriteLine($"Duration remainder: {durationRemainder}");
                    duration++;
                    durationRemainder--;
                }

                Console.WriteLine($"Note: Time passed: {measurePassed}:{beatPassed}:{divisionPassed}, Duration: {duration}");

                // Note
                int pitchPos = line.IndexOf(searchPitch) + searchPitch.Length;
                string pitch = line.Substring(pitchPos, 3).Replace(" ", "").Replace('#', 'S').ToUpper();
                string note = $"NOTE_{pitch}";

                switch (currentTrack)
                {
                    case 1:
                        notes1.Add(note);
                        durations1.Add(duration);
                        break;

                    case 2:
                        notes2.Add(note);
                        durations2.Add(duration);
                        break;
                }

                string timeSig = currentTimeSigPart1 + "/" + currentTimeSigPart2;
                Console.WriteLine($"Time: {time}, Time Sig: {timeSig}, Tempo {currentTempo}, Off Note: {note}, Duration: {duration}");

                lastOffNoteMeasure = currentMeasure;
                lastOffNoteBeat = currentBeat;
                lastOffNoteDivision = currentDivision;
                lastOffNoteTimeSigPart1 = currentTimeSigPart1;
                lastOffNoteTimeSigPart2 = currentTimeSigPart2;
                lastOffNoteTempo = currentTempo;
            }
        }
    }
}

// Small pause after playing song + equal durations on tracks
notes1.Add("0");
notes2.Add("0");
int pause = 2000;
int totalDurations1 = durations1.Sum();
int totalDurations2 = durations2.Sum();
if (totalDurations1 > totalDurations2)
{
    durations1.Add(pause);
    durations2.Add(totalDurations1 - totalDurations2 + pause);
}
else if (totalDurations1 < totalDurations2)
{
    durations1.Add(totalDurations2 - totalDurations1 + pause);
    durations2.Add(pause);
}
else
{
    durations1.Add(pause);
    durations2.Add(pause);
}

int size1 = notes1.Count;
int size2 = notes2.Count;

using (StreamWriter writer = new StreamWriter(exportedFilePath))
{
    writer.WriteLine($"#ifndef {exportedFileNameWithoutExtension.ToUpper()}_H");
    writer.WriteLine($"#define {exportedFileNameWithoutExtension.ToUpper()}_H");
    writer.WriteLine("");
    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Size1 = {size1};");
    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Size2 = {size2};");
    writer.WriteLine("");

    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Notes1[] PROGMEM = {{");
    writer.Write("  ");
    for (int i = 0; i < notes1.Count; i++)
    {
        if (i > 0) writer.Write(", ");
        writer.Write(notes1[i]);
    }
    writer.WriteLine();
    writer.WriteLine("};");

    writer.WriteLine("");

    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Durations1[] PROGMEM = {{");
    writer.Write("  ");
    for (int i = 0; i < durations1.Count; i++)
    {
        if (i > 0) writer.Write(", ");
        writer.Write(durations1[i]);
    }
    writer.WriteLine();
    writer.WriteLine("};");

    writer.WriteLine("");

    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Notes2[] PROGMEM = {{");
    writer.Write("  ");
    for (int i = 0; i < notes2.Count; i++)
    {
        if (i > 0) writer.Write(", ");
        writer.Write(notes2[i]);
    }
    writer.WriteLine();
    writer.WriteLine("};");

    writer.WriteLine("");

    writer.WriteLine($"const int {exportedFileNameWithoutExtension}_Durations2[] PROGMEM = {{");
    writer.Write("  ");
    for (int i = 0; i < durations2.Count; i++)
    {
        if (i > 0) writer.Write(", ");
        writer.Write(durations2[i]);
    }
    writer.WriteLine();
    writer.WriteLine("};");

    writer.WriteLine("");
    writer.WriteLine("#endif");
    writer.WriteLine("");
}

Console.WriteLine("");
Console.WriteLine("Export done!");
Console.WriteLine(exportedFileName);
Console.WriteLine(exportedFilePath);
Console.ReadKey();