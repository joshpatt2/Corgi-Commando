using System;
using System.Collections;
using CorgiCommando.Core;
using UnityEngine;

namespace CorgiCommando.Testing
{
    public class PlaytestBot : MonoBehaviour
    {
        public IEnumerator Play(IInputBuffer buffer, PlaytestScript script)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            using (var recorder = new PlaytestRecorder())
            {
                recorder.StartRun();

                float startTime = Time.unscaledTime;
                for (int i = 0; i < script.entries.Count; i++)
                {
                    PlaytestEntry entry = script.entries[i];
                    float targetTime = startTime + Mathf.Max(0f, entry.timestamp);
                    while (Time.unscaledTime < targetTime)
                    {
                        yield return null;
                    }

                    buffer.RecordInput(entry.action, targetTime, entry.axisValue);
                }
            }
        }
    }
}
