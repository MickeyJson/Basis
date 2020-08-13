using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public enum ChangeType
{
    Linear,
    Easein,
    Easeout
}
public class NumberEaseChange : MonoBehaviour
{
    int result = 0;

    private int start = 1;
    private int end = 500;

    public float jumpTimes = 2;
    private float nowtime = 0;

    private Text label = null;
    Timer timer;
    public ChangeType changeType = ChangeType.Linear;
    float ss;
    void Start()
    {
        label = gameObject.GetComponent<Text>();
        timer = TimerManager.Instance.AddTimer(0f, jumpTimes * 10, 0f, false, JumpNumber);
        ss = System.DateTime.Now.Second;
    }

    public void ChangeNumber(int endNum, float duration)
    {
        end = endNum;
        jumpTimes = duration;
        timer = TimerManager.Instance.AddTimer(0.1f, jumpTimes * 10, 0f, false, JumpNumber);
    }

    public void JumpNumber()
    {
        result = 0;
        nowtime += 0.1f;
        if (changeType == ChangeType.Linear)
        {
            result += UniformMotion(start, end, nowtime, jumpTimes);
        }
        else if (changeType == ChangeType.Easein)
        {
            result += EaseIn(start, end, nowtime, jumpTimes);
        }
        else if (changeType == ChangeType.Easeout)
        {
            result += EaseOut(start, end, nowtime, jumpTimes);
        }
        if(result>end)
        {
            result = end;
        }
        label.text = result.ToString();
        if (result == end)
        {
            Debug.LogError(result);

            Debug.LogError(System.DateTime.Now.Second - ss);
        }
    }


    /// <summary>
    ///  匀速
    /// </summary>
    /// <param name="origin">要变换的起始值</param>
    /// <param name="transform">要变换的总值</param>
    /// <param name="usedTime">已进行动画时间</param>
    /// <param name="allTime">总动画时间</param>
    /// <returns></returns>
    public static int UniformMotion(int start, int end, float usedTime, float allTime)
    {
        return start + (int)(end * UniformMotionCore(usedTime, allTime));
    }
    /// <summary>
    /// 匀速
    /// </summary>
    /// <param name="usedTime">已进行动画时间</param>
    /// <param name="allTime">总动画时间</param>
    /// <returns></returns>
    public static float UniformMotionCore(float usedTime, float allTime)
    {
        return usedTime / allTime;
    }

    /// <summary>
    /// 变速
    /// </summary>
    /// <param name="origin">要变换的起始值</param>
    /// <param name="transform">要变换的总值</param>
    /// <param name="usedTime">已进行动画时间</param>
    /// <param name="allTime">总动画时间</param>
    /// <param name="power">动画曲线幂(默认值2)</param>
    /// <returns></returns>
    public static int EaseIn(int start, int end, float usedTime, float allTime, int power = 2)
    {
        return start + (int)(end * EaseInCore(usedTime / allTime, power));
    }

    /// <summary>
    /// 变速
    /// </summary>
    /// <param name="origin">要变换的起始值</param>
    /// <param name="transform">要变换的总值</param>
    /// <param name="usedTime">已进行动画时间</param>
    /// <param name="allTime">总动画时间</param>
    /// <param name="power">动画曲线幂(默认值2)</param>
    /// <returns></returns>
    public static int EaseOut(int start, int end, float usedTime, float allTime, int power = 2)
    {
        return start + (int)(end * (EaseOutCore(usedTime / allTime, power)));
    }

    /// <summary>
    /// 变速
    /// </summary>
    /// <param name="progressTime">已进行动画时间/总动画时间</param>
    /// <param name="power">动画曲线幂(默认值2)</param>
    /// <returns></returns>
    public static double EaseInCore(float progressTime, float power)
    {
        power = Mathf.Max(0.0f, power);
        return Mathf.Pow(progressTime, power);
    }

    /// <summary>
    /// 变速
    /// </summary>
    /// <param name="progressTime">已进行动画时间/总动画时间</param>
    /// <param name="power">动画曲线幂(默认值2)</param>
    /// <returns></returns>    
    public static double EaseOutCore(float progressTime, int power)
    {
        return 1 - EaseInCore(1.0f - progressTime, power);
    }

    public void Reset()
    {
        TimerManager.Instance.CancelTimer(timer);
        start = 0;
        nowtime = 0;
        timer = TimerManager.Instance.AddTimer(0.1f, jumpTimes * 10, 0f, false, JumpNumber);
    }
}

public class TimerManager : MonoBehaviour
{
    private static TimerManager _instance;
    public static TimerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("TimerManager");
                _instance = obj.AddComponent<TimerManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    private List<Timer> _usedTimes = new List<Timer>();
    private Queue<Timer> _canceledTimes = new Queue<Timer>();

    public void Update()
    {
        int count = _usedTimes.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            _usedTimes[i].Uptate();
        }
    }
    public void CancelTimer(Timer timer)
    {
        _usedTimes.Remove(timer);

        if (_canceledTimes.Count < 10)
        {
            timer.Cancel();
            _canceledTimes.Enqueue(timer);
        }
    }
    public Timer AddTimer(float delayTime, Action excuteFunc)
    {
        return AddTimer(delayTime, 1, delayTime, false, excuteFunc, null, null);
    }
    public Timer AddTimer(float duration, int loopTimes, Action excuteFunc)
    {
        return AddTimer(duration, loopTimes, duration, false, excuteFunc, null, null);
    }
    public Timer AddTimer(float duration, float loopTimes, float delayTime, Action excuteFunc)
    {
        return AddTimer(duration, loopTimes, delayTime, false, excuteFunc, null, null);
    }
    public Timer AddTimer(float duration, float loopTimes, float delayTime, bool isRealTime, Action excuteFunc)
    {
        return AddTimer(duration, loopTimes, delayTime, isRealTime, excuteFunc, null, null);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="duration">2次执行的时间间隔</param>
    /// <param name="loopTimes">循环几次</param>
    /// <param name="delayTime">第一次的延迟</param>
    /// <param name="isRealTime">是否真实时间</param>
    /// <param name="excuteFunc">执行体</param>
    /// <param name="completeFunc">完成调用</param>
    /// <param name="finishFunc">取消时调用</param>
    /// <returns></returns>
    public Timer AddTimer(float duration, float loopTimes, float delayTime, bool isRealTime, Action excuteFunc, Action completeFunc, Action finishFunc)
    {
        Timer timer = null;
        if (_canceledTimes.Count > 0)
        {
            timer = _canceledTimes.Dequeue();
        }
        else
            timer = new Timer(this);

        timer.Create(duration, loopTimes, delayTime, isRealTime, excuteFunc, completeFunc, finishFunc);
        _usedTimes.Add(timer);

        return timer;
    }
}

public class Timer
{
    public Timer(TimerManager tm)
    {
        timerManager = tm;
    }

    public float duration;
    public float loopTimes;
    public float delayTime;
    public bool isRealTime;
    public Action completeFunc;
    public Action finishFunc;
    public Action excuteFunc;

    private TimerManager timerManager;
    private float _lastDoneTime;
    private float _startTime;
    public void Create(float duration, float loopTimes, float delayTime, bool isRealTime, Action excuteFunc, Action completeFunc, Action finishFunc)
    {
        this.duration = duration;
        this.loopTimes = loopTimes;
        this.delayTime = delayTime;
        this.isRealTime = isRealTime;
        this.completeFunc = completeFunc;
        this.finishFunc = finishFunc;
        this.excuteFunc = excuteFunc;

        float curTime = GetTime();
        _startTime = curTime + delayTime;
    }
    public void Uptate()
    {
        float curTime = GetTime();
        if (_lastDoneTime < 0)
        {
            // 第一次执行
            if (curTime >= _startTime)
            {
                if (excuteFunc != null)
                    excuteFunc();

                _lastDoneTime = curTime;
                if (loopTimes != -1)
                {
                    loopTimes--;
                    if (loopTimes == 0)
                    {
                        if (finishFunc != null)
                            finishFunc();

                        timerManager.CancelTimer(this);
                    }
                }
            }
        }
        else
        {
            // 循环执行
            if (curTime - _lastDoneTime >= duration)
            {
                if (excuteFunc != null)
                    excuteFunc();

                _lastDoneTime = curTime;
                if (loopTimes != -1)
                {
                    loopTimes--;
                    if (loopTimes == 0)
                    {
                        if (finishFunc != null)
                            finishFunc();

                        timerManager.CancelTimer(this);
                    }
                }
            }
        }
    }
    private float GetTime()
    {
        if (isRealTime)
            return Time.realtimeSinceStartup;
        else
            return Time.time;
    }
    public void Cancel()
    {
        _lastDoneTime = -1;
        _startTime = 0;
    }
}