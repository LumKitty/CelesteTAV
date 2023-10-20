using System.Collections.Generic;
using System.IO;
using System.Linq;
using StudioCommunication;
using TAS.Communication;
using TAS.Utils;

namespace TAS.Input.Commands;

public static class RecordingCommand {
    internal record RecordingTime {
        public int StartFrame = int.MaxValue;
        public int StopFrame = int.MaxValue;
        public int Duration => StopFrame - StartFrame;
    }

    internal static readonly Dictionary<int, RecordingTime> RecordingTimes = new();

    // workaround the first few frames get skipped when there is a breakpoint after StartRecording command
    public static bool StopFastForward {
        get {
            if (Manager.Recording) {
                return true;
            }

            return RecordingTimes.Values.Any(time => {
                int currentFrame = Manager.Controller.CurrentFrameInTas;
                return currentFrame > time.StartFrame - 60 && currentFrame <= time.StopFrame;
            });
        }
    }

    // "StartRecording"
    [TasCommand("StartRecording", ExecuteTiming = ExecuteTiming.Parse | ExecuteTiming.Runtime)]
    private static void StartRecording(string[] _1, int _2, string filePath, int fileLine) {
        if (ParsingCommand) {
            if (StudioCommunicationBase.Initialized && Manager.Running) {
                if (!TASRecorderUtils.Installed) {
                    StudioCommunicationClient.Instance?.SendRecordingFailed(RecordingFailedReason.TASRecorderNotInstalled);
                } else if (!TASRecorderUtils.FFmpegInstalled) {
                    StudioCommunicationClient.Instance?.SendRecordingFailed(RecordingFailedReason.FFmpegNotInstalled);
                }
            }

            if (!TASRecorderUtils.Installed) {
                AbortTas("TAS Recorder isn't installed");
                return;
            }
            if (!TASRecorderUtils.FFmpegInstalled) {
                AbortTas("FFmpeg libraries aren't properly installed");
                return;
            }
            
            if (RecordingTimes.Count > 0) {
                RecordingTime last = RecordingTimes.Last().Value;
                if (last.StopFrame == int.MaxValue) {
                    string errorText = $"{Path.GetFileName(filePath)} line {fileLine}\n";
                    AbortTas($"{errorText}StopRecording is required before another StartRecording");
                    return;
                }
            }

            RecordingTime time = new() { StartFrame = Manager.Controller.Inputs.Count };
            RecordingTimes[time.StartFrame] = time;
        } else {
            if (Manager.Recording) {
                AbortTas("Tried to start recording, while already recording");
                return;
            }

            TASRecorderUtils.StartRecording();
            if (RecordingTimes.TryGetValue(Manager.Controller.CurrentFrameInTas, out RecordingTime time) &&
                       time.StartFrame != int.MaxValue && time.StopFrame != int.MaxValue) {
                TASRecorderUtils.SetDurationEstimate(time.Duration);
            }

            Manager.States &= ~States.FrameStep;
            Manager.NextStates &= ~States.FrameStep;
        }
    }

    // "StopRecording"
    [TasCommand("StopRecording", ExecuteTiming = ExecuteTiming.Parse | ExecuteTiming.Runtime)]
    private static void StopRecording(string[] _1, int _2, string filePath, int fileLine) {
        if (ParsingCommand) {
            string errorText = $"{Path.GetFileName(filePath)} line {fileLine}\n";
            if (RecordingTimes.IsEmpty()) {
                AbortTas($"{errorText}StartRecording is required before StopRecording");
                return;
            }

            RecordingTime last = RecordingTimes.Last().Value;

            if (last.StopFrame != int.MaxValue) {
                if (last.StopFrame == int.MaxValue) {
                    AbortTas($"{errorText}StartRecording is required before another StopRecording");
                    return;
                }
            }
            
            last.StopFrame = Manager.Controller.Inputs.Count;
        } else {
            TASRecorderUtils.StopRecording();
        }
    }
    
    [ParseFileEnd]
    private static void ParseFileEnd() {
        if (RecordingTimes.IsEmpty()) {
            return;
        }

        RecordingTime last = RecordingTimes.Last().Value;
        if (last.StopFrame == int.MaxValue) {
            last.StopFrame = Manager.Controller.Inputs.Count;
        }
    }

    [ClearInputs]
    private static void Clear() {
        RecordingTimes.Clear();
    }

    [DisableRun]
    private static void DisableRun() {
        if (Manager.Recording) {
            TASRecorderUtils.StopRecording();
        }
    }
}