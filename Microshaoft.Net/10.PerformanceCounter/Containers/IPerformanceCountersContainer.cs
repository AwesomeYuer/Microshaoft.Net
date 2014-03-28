namespace Microshaoft
{
    using System.Diagnostics;
    public interface IPerformanceCountersContainer
    {
         void AttachPerformanceCountersToProperties
                        (
                            string performanceCountersCategoryInstanceName
                            , string performanceCountersCategoryName
                        );
         PerformanceCounter PrcocessPerformanceCounter
         {
             get;
         }
         PerformanceCounter ProcessingPerformanceCounter
         {
             get;
         }
         PerformanceCounter ProcessedPerformanceCounter
         {
             get;
         }
         PerformanceCounter ProcessedRateOfCountsPerSecondPerformanceCounter
         {
             get;
         }
         PerformanceCounter ProcessedAverageTimerPerformanceCounter
         {
             get;
         }
         PerformanceCounter ProcessedAverageBasePerformanceCounter
         {
             get;
         }
         PerformanceCounter CaughtExceptionsPerformanceCounter
         {
             get;
         }
        
    }
}
