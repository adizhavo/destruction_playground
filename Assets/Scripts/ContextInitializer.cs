using System.Collections.Generic;
using GramGames.Framework.Context;
using GramGames.Framework.Core;
using GramGames.Framework.GGAnalytics.SDK;
using GramGames.Framework.Services.ABTest;
using GramGames.Framework.Services.Analytics;

namespace GramGames
{
	public static class ContextInitializer
	{
		public static void Initialize()
		{
			ContextBuilder.Create().BeginContext(AnalyticsServiceConsts.Context.NAME)
			#if ADHOC && UNITY_EDITOR
			.PutData(AnalyticsServiceConsts.Context.KEY__ENVIRONMENT, GGEnvironment.DEVELOPMENT)
			#elif ADHOC && !UNITY_EDITOR
			.PutData(AnalyticsServiceConsts.Context.KEY__ENVIRONMENT, GGEnvironment.TEST)
							#elif !ADHOC && !UNITY_EDITOR
			.PutData(AnalyticsServiceConsts.Context.KEY__ENVIRONMENT, GGEnvironment.PRODUCTION)
							#endif
			#if UNITY_EDITOR
			.PutData(AnalyticsServiceConsts.Context.KEY__BUFFER_TYPE, BufferType.SYNC)
			#else
            .PutData(AnalyticsServiceConsts.Context.KEY__BUFFER_TYPE, BufferType.ASYNC)
							#endif
			.PutData(AnalyticsServiceConsts.Context.KEY__APPLICATION_ID, GGConstants.Config.APPLICATION_ID)
			.PutData(AnalyticsServiceConsts.Context.KEY__ADJUST_APP_TOKEN, GGConstants.Config.ADJUST_APP_TOKEN)
			.PutData(AnalyticsServiceConsts.Context.KEY__ADJUST_ATTRIBUTION_EVENT_TOKEN, "")
			.PutData(AnalyticsServiceConsts.Context.KEY__ADJUST_REVENUE_EVENT_TOKEN, GGConstants.Config.ADJUST_REVENUE_TOKEN)
			.PutData(AnalyticsServiceConsts.Context.KEY__ADJUST_CUSTOM_EVENT_TOKEN_MAP, new Dictionary<string, string> { })
			.EndContext();
		}
	}
}
