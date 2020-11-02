using System;
using System.Collections.Generic;
using GramGames;
using GramGames.Framework.Core.App;
using GramGames.Framework.Core.EventSystem;
using GramGames.Framework.Services;
using GramGames.Framework.Services.ABTest;
using GramGames.Framework.Services.Database;
using GramGames.Framework.Services.Analytics;
using GramGames.Framework.Services.User;
using UnityEngine;

public static class GameServices
{
    private static Action callback;

    public static void Initialize(Action i_callback)
    {
        callback = i_callback;

        EventDispatcher.AddEventListener(CoreAppConsts.GGEvent.GGAPP_INIT_COMPLETE, InitGGAppCallback, true);
        ContextInitializer.Initialize();
        GGApp.Init(new GGAppConfig(GGConstants.Config.APPLICATION_ID, OnABRegisterSuccess, OnABRegisterFail, OnABExitTest, OnABTestBootFlowCompleted, DeleteDataFromDevice));
    }

    public static void InitGGAppCallback(string i_eventType, Dictionary<string, object> i_payload)
    {
        if (callback != null)
        {
            callback();
        }

        callback = null;
    }

    public static IPersistencyService DB
    {
        get { return ServiceLocator.Instance.GetService<IPersistencyService>(); }
    }


    public static IUserService User
    {
        get { return ServiceLocator.Instance.GetService<IUserService>(); }
    }

    public static IAnalyticsService Analytics
    {
        get { return ServiceLocator.Instance.GetService<IAnalyticsService>(); }
    }


    private static void DeleteDataFromDevice()
    {
        Analytics.DestroyAnalyticsData();
        DB.DeleteAll();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    private static void OnABRegisterSuccess(ABTestInstanceData i_testInstanceData)
    {
    }

    private static void OnABRegisterFail(int i_errorCode, string i_errorMessage)
    {
    }

    private static void OnABExitTest(string i_testKey, string i_extraPayload = null)
    {
    }

    private static void OnABTestBootFlowCompleted()
    {
    }
}