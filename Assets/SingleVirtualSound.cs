using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SingleVirtualSound : MonoBehaviour
{
    static UnityEvent OnFmodEnter;

    //public
    public int athmoIndex;
    public TimelineInfo timelineInfo = null;
    public delegate void fmodDelegateTest();
    public static fmodDelegateTest fmodDel;

    //private
    Vector3 thisOnesPos;
    GCHandle timelineHandle;
    string soundContainer;
    float d01 =0;
    float d02 =0;

    //FMOD
    public FMODUnity.EventReference EventName;
    FMOD.Studio.EVENT_CALLBACK beatCallback;
    FMOD.Studio.EventInstance soundInstances;

    [StructLayout(LayoutKind.Sequential)]
    public class TimelineInfo
    {
        public FMOD.Studio.EVENT_CALLBACK_TYPE eventType;
    }


    private void Awake()
    {
        thisOnesPos = transform.position;
    }

    void Start()
    {   timelineInfo = new TimelineInfo();
        beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);

        soundInstances = FMODUnity.RuntimeManager.CreateInstance("event:/Athmos");

        timelineHandle = GCHandle.Alloc(timelineInfo, GCHandleType.Pinned);
        soundInstances.setUserData(GCHandle.ToIntPtr(timelineHandle));
        soundInstances.setCallback(beatCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.VIRTUAL_TO_REAL | FMOD.Studio.EVENT_CALLBACK_TYPE.REAL_TO_VIRTUAL);
        soundInstances.start();

        OnFmodEnter = new UnityEvent();
        fmodDel = fmodDelFun;
        OnFmodEnter.AddListener(EventChanges); //Event to change and use non-static variables
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(soundInstances, this.transform);
    }
    static void fmodDelFun()
    {
        OnFmodEnter.Invoke();
    }
    void OnDestroy()
    {
        soundInstances.setUserData(System.IntPtr.Zero);
        soundInstances.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        soundInstances.release();
        timelineHandle.Free();
        OnFmodEnter.RemoveAllListeners();
    }

    void EventChanges()
    {
/*        FMOD.ATTRIBUTES_3D res;
        FMODUnity.RuntimeManager.StudioSystem.getListenerAttributes(0, out res);
        Vector3 v01 = FmodV3ToV3(res.position);
        FMODUnity.RuntimeManager.StudioSystem.getListenerAttributes(1, out res);
        Vector3 v02 = FmodV3ToV3(res.position);
        d01 = Vector3.Distance(thisOnesPos, v01);
        d02 = Vector3.Distance(thisOnesPos, v02);*/
       // print(d01 + " " + d02);
        
    }


    Vector3 FmodV3ToV3(FMOD.VECTOR v)
    {
        Vector3 uv = new Vector3(v.x, v.y, v.z);
        return uv;
    }

    private void Update()
    {
        if (timelineInfo.eventType == FMOD.Studio.EVENT_CALLBACK_TYPE.VIRTUAL_TO_REAL && soundContainer != this.name)
        {
            FMOD.ATTRIBUTES_3D res;
            FMODUnity.RuntimeManager.StudioSystem.getListenerAttributes(0, out res);
            Vector3 v01 = FmodV3ToV3(res.position);
            FMODUnity.RuntimeManager.StudioSystem.getListenerAttributes(1, out res);
            Vector3 v02 = FmodV3ToV3(res.position);
            d01 = Vector3.Distance(thisOnesPos, v01);
            d02 = Vector3.Distance(thisOnesPos, v02);

            soundInstances.setParameterByName("AthmoIndex", athmoIndex);
            print(d01 + " - " + d02 + " so " + (d01<d02));

            soundInstances.setParameterByName("Pan", (d01 < d02) ? 0 : 1);
            soundContainer = this.name;
            GetComponent<Renderer>().material.color = Color.black;
        }
        if (timelineInfo.eventType == FMOD.Studio.EVENT_CALLBACK_TYPE.REAL_TO_VIRTUAL && soundContainer == this.name)
        {
            soundContainer = null;
            GetComponent<Renderer>().material.color = Color.green;
        }
    }

    void OnGUI()
    {
        GUILayout.Box(System.String.Format("Current Object = {0}", soundContainer));//
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT BeatEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    {
       // if (type != FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT)  Debug.Log(type);

        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);

        System.IntPtr timelineInfoPtr;
        FMOD.RESULT result = instance.getUserData(out timelineInfoPtr);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("Timeline Callback error: " + result);
        }

        else if (timelineInfoPtr != System.IntPtr.Zero)
        {
            // Get the object to store beat and marker details
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;

            switch (type)
            {
                case FMOD.Studio.EVENT_CALLBACK_TYPE.REAL_TO_VIRTUAL:
                    {
                        timelineInfo.eventType = type;
                    }
                    break;
                case FMOD.Studio.EVENT_CALLBACK_TYPE.VIRTUAL_TO_REAL:
                    {
                        timelineInfo.eventType = type;
                        fmodDel();
                    }
                    break;
                case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
                    {
                        // Now the event has been destroyed, unpin the timeline memory so it can be garbage collected
                        timelineHandle.Free();
                    }
                    break;
            }
        }
        return FMOD.RESULT.OK;
    }

}
