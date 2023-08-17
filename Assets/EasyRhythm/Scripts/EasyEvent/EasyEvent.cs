using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System;
using FMOD;
using FMOD.Studio;
using Debug = UnityEngine.Debug;
//[Serializable]
public class EasyEvent
{
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    private EventInstance eventInstance;

    private int currentMusicBeat_Temp = 0; // used to store current music beat temporarily, to check if the beat has updated
    private string lastMarker_Temp = ""; // used to store current marker beat temporarily, to check if the marker has updated
    private float lastMarkerPos_Temp = 0; // used to store current marker position temporarily, to check if the marker has updated

//    [FMODUnity.EventRef]
//    public string path; // location of event
    public FMODUnity.EventReference path;
    private string eventName;
    public string EventName { get => eventName; }

    public bool ListenForMarkers = true;
    [HideInInspector]
    public List<IEasyListener> listeners = new List<IEasyListener>();

    // EASY ACCESS TO TIMELINE INFO

    private int currentBeat;
    private int currentBar;
    private float currentTempo;
    private string lastMarker;
    private float lastMarkerPos;
    private float currentTimelinePosition;
    private int timeSignatureUpper;
    private int timeSignatureLower;

    public int CurrentBeat { get => currentBeat; set => currentBeat = value; }
    public int CurrentBar { get => currentBar; set => currentBar = value; }
    public float CurrentTempo { get => currentTempo; set => currentTempo = value; }
    public string LastMarker { get => lastMarker; set => lastMarker = value; }
    public float LastMarkerPos { get => lastMarkerPos; set => lastMarkerPos = value; }
    public float CurrentTimelinePosition { get => currentTimelinePosition; set => currentTimelinePosition = value; }
    public int TimeSignatureUpper { get => timeSignatureUpper; set => timeSignatureUpper = value; }
    public int TimeSignatureLower { get => timeSignatureLower; set => timeSignatureLower = value; }

    /// <summary>
    /// A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    public EasyEvent(FMODUnity.EventReference eventPath)
    {
        path = eventPath;
        Setup();
    }

    /// <summary>
    /// A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    /// <param name="eventListener">An object that will listen for the beat and FMOD markers.</param>
    /// <param name="listenForMarkers">Whether a method should be invoked when a matching FMOD marker is passed.</param>
    public EasyEvent(FMODUnity.EventReference eventPath, UnityEngine.Object eventListener, bool listenForMarkers = true)
    {
        if (eventListener is IEasyListener)
        {
            AddListener((IEasyListener)eventListener);
        }

        path = eventPath;
        ListenForMarkers = listenForMarkers;
        Setup();
    }

    /// <summary>
    /// A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    /// <param name="eventListeners">A list of objects that will listen for the beat and FMOD markers.</param>
    /// <param name="listenForMarkers">Whether a method should be invoked when a matching FMOD marker is passed.</param>
    public EasyEvent(FMODUnity.EventReference eventPath, UnityEngine.Object[] eventListeners, bool listenForMarkers = true)
    {
        foreach(UnityEngine.Object listener in eventListeners)
        {
            if (listener is IEasyListener)
            {
                AddListener((IEasyListener)listener);
            }
        }

        path = eventPath;
        //AddListeners(eventListeners);
        ListenForMarkers = listenForMarkers;
        Setup();
    }

    public void Setup()
    {
        if (EasyEventUpdater.instance == null)
        {
            var updater = new GameObject("EasyEvent Updater");
            updater.AddComponent<EasyEventUpdater>();
        }

        LastMarker = new FMOD.StringWrapper();
        beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);

        eventInstance = FMODUnity.RuntimeManager.CreateInstance(path); // creates an instance of the music event

        eventInstance.setCallback(beatCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT | FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        // Gets name of FMOD event
        var tempString = path.ToString().Split('/');
        eventName = tempString[tempString.Length - 1];

        EasyEventUpdater.OnUpdate += Update;
    }


/// See https://www.fmod.com/docs/2.02/unity/examples-timeline-callbacks.html for original implementation
/// Updated 'BeatEventCallback' to match delegate 'EVENT_CALLBACK' for FMod 2.02

/// List of all possible callback types if you want to extend this: 
/// FMOD_STUDIO_EVENT_CALLBACK_TYPE
///  CREATED                  = 0x00000001,
///  DESTROYED                = 0x00000002,
///  STARTING                 = 0x00000004,
///  STARTED                  = 0x00000008,
///  RESTARTED                = 0x00000010,
///  STOPPED                  = 0x00000020,
///  START_FAILED             = 0x00000040,
///  CREATE_PROGRAMMER_SOUND  = 0x00000080,
///  DESTROY_PROGRAMMER_SOUND = 0x00000100,
///  PLUGIN_CREATED           = 0x00000200,
///  PLUGIN_DESTROYED         = 0x00000400,
///  TIMELINE_MARKER          = 0x00000800,
///  TIMELINE_BEAT            = 0x00001000,
///  SOUND_PLAYED             = 0x00002000,
///  SOUND_STOPPED            = 0x00004000,
///  REAL_TO_VIRTUAL          = 0x00008000,
///  VIRTUAL_TO_REAL          = 0x00010000,
///  START_EVENT_COMMAND      = 0x00020000,
///  NESTED_TIMELINE_BEAT     = 0x00040000,
///  ALL                      = 0xFFFFFFFF,

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    public FMOD.RESULT BeatEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
        // Retrieve the user data
        IntPtr timelineInfoPtr;
        

        instance.getUserData(out timelineInfoPtr);
        FMOD.RESULT result = instance.getUserData(out timelineInfoPtr);


        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("Timeline Callback error: " + result);
        }
        //else if (timelineInfoPtr != IntPtr.Zero)
        //{
            // Get the object to store beat and marker details
            //GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            //TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;
        //}
#if UNITY_EDITOR
        else
        {
            Debug.Log("BeatEventCallback timelineInfoPtr is IntPtr.Zero");
        }
#endif

        switch (type)
        {
            case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
            {

                var parameter = (FMOD.Studio.TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_BEAT_PROPERTIES));
                currentBeat = parameter.beat; // current beat number (eg. 1-4)
                currentBar = parameter.bar; // current bar number
                currentTempo = parameter.tempo; // current tempo
                currentTimelinePosition = parameter.position; // current playback position
                timeSignatureUpper = parameter.timesignatureupper;
                timeSignatureLower = parameter.timesignaturelower;
                break;
            }
            case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
            {

                var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                lastMarker = parameter.name;
                lastMarkerPos = parameter.position;
                break;
            }
            case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
            {
                // Now the event has been destroyed, unpin the timeline memory so it can be garbage collected
                if (timelineInfoPtr != IntPtr.Zero)
                {
                // Get the object to store beat and marker details
                    GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
                    timelineHandle.Free();
                }
                break;
            }
#if UNITY_EDITOR
            default:
                Debug.Log("BeatEventCallback no case for " + type);
            break;
#endif 
        }
      
        return FMOD.RESULT.OK;
    }


    /// <summary>
    /// Adds a listener to this audio event.
    /// The listener will hear OnBeat() and invoke any methods that match FMOD markers.
    /// </summary>
    /// <param name="listener"></param>
    public void AddListener(IEasyListener listener)
    {
        listeners.Add(listener);
    }

    public void AddListeners(UnityEngine.Object[] newListeners)
    {
        foreach (UnityEngine.Object listener in newListeners)
        {
            if (listener is IEasyListener)
            {
                listeners.Add((IEasyListener)listener);
            }
        }
    }

    /// <summary>
    /// Removes a listener from this audio event.
    /// The listener will no longer respond to OnBeat() or any FMOD Markers.
    /// </summary>
    /// <param name="listener"></param>
    public void RemoveListener(IEasyListener listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>
    /// Removes an array of listeners from this audio event.
    /// </summary>
    /// <param name="listenersToRemove"></param>
    public void RemoveListeners(IEasyListener[] listenersToRemove)
    {
        foreach (IEasyListener listener in listenersToRemove)
        {
            listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Removes all listeners from this audio event.
    /// </summary>
    public void RemoveAllListeners()
    {
        listeners.Clear();
    }

    public void start()
    {
        eventInstance.start();
    }

    public void SetPaused(bool paused)
    {
        eventInstance.setPaused(paused);
    }

    /// <summary>
    /// Stops the audio event.
    /// </summary>
    /// <param name="allowFadeOut"></param>
    public void stop(bool allowFadeOut = true)
    {
        FMOD.Studio.STOP_MODE stopMode = allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE;

        eventInstance.stop(stopMode);
    }

    public void Update()
    {
        if (IsPlaying())
        {
            CheckNewMarker();
            CheckOnBeat();
        }
    }

    public bool IsPlaying()
    {
        FMOD.Studio.PLAYBACK_STATE state;

        eventInstance.getPlaybackState(out state);

        return state == FMOD.Studio.PLAYBACK_STATE.PLAYING ? true : false;
    }

    // Checks if a new marker has been passed
    private void CheckNewMarker()
    {
        if (LastMarkerPos != lastMarkerPos_Temp)
        {
            lastMarkerPos_Temp = LastMarkerPos; // update temp marker value

            string markerName = LastMarker;
            lastMarker_Temp = markerName;

            //if (!ListenForMarkers) return;

            CheckInvokeFromMarker(markerName);
        }
    }

    // If a matching method from the marker is found in the listener it will be invoked
    private void CheckInvokeFromMarker(string marker)
    {
        if (marker.Contains(" "))
        {
            var strings = marker.Split(' ');
            marker = null;

            foreach (string stringPart in strings)
            {
                marker += stringPart;
            }
        }

        foreach (MonoBehaviour listener in listeners)
        {
            System.Type T = listener.GetType();
            foreach (System.Reflection.MethodInfo m in T.GetMethods())
            {
                if (m.Name == marker)
                {
                    listener.SendMessage(marker, this);
                    break;
                }
            }
        }
    }

    private void CheckOnBeat()
    {
        // Checks if beat has changed and calls OnBeat event to all subscribers
        if (currentMusicBeat_Temp != CurrentBeat)
        {
            currentMusicBeat_Temp = CurrentBeat;

            if (listeners == null || listeners.Count <= 0){

                return;
            }

            foreach (IEasyListener listener in listeners)
            {

                listener.OnBeat(this);
            }
        }
    }

    /// <summary>
    /// Returns an array representing the time signature.
    /// Index 0 represents the value of the upper part of the time signature
    /// Index 1 represents the value of the lower part of the time signature
    /// </summary>
    /// <returns></returns>
    public int[] TimeSigAsArray()
    {
        return new int[] { TimeSignatureUpper, TimeSignatureLower };
    }

    /// <summary>
    /// Returns a string representing the time signature
    /// eg. 4/4
    /// </summary>
    /// <returns></returns>
    public string TimeSigAsString()
    {
        return TimeSignatureUpper.ToString() + "/" + TimeSignatureLower.ToString();
    }

    // Returns length of beat
    public float BeatLength()
    {
        float beatLength = 60 / CurrentTempo;

        if (beatLength == Mathf.Infinity) return 0; // Stops from return infinity at beginning of song

        return beatLength;
    }

    void OnDestroy()
    {
        eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        EasyEventUpdater.OnUpdate -= Update;
        RemoveAllListeners();
        eventInstance.setUserData(IntPtr.Zero);
        eventInstance.release();
    }
}
