using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(AccessRights = AccessRights.FullAccess)]
    public class ZrIndEngulfingCandle : Indicator
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        protected override void Initialize()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate
            var result = System.Diagnostics.Debugger.Launch();
            Print(Message);
        }

        int standardDeviationSampleNumber = 25;
        double tollerancePercent = 0;
        double bodyPercentVsWick = 200;
        double sdThreshold = 2.5;
        bool logTrendlineCrossing = true;
        public override void Calculate(int index)
        {
            var bars = MarketData.GetBars(this.TimeFrame);


            //Get trendline drawing on chart and check delta price distance from line
            if (logTrendlineCrossing)
            {
                var equidistantChannels = Chart.FindAllObjects(ChartObjectType.EquidistantChannel);
                foreach (var item in equidistantChannels)
                {
                    Debug.WriteLine($"item name {item.Comment} channel height: {((ChartEquidistantChannel)item).ChannelHeight}");
                }

                var trendLines = Chart.FindAllObjects(ChartObjectType.TrendLine);
                foreach (ChartTrendLine item in trendLines)
                {                   
                    var barCourrent = bars.TakeLast(1).First();
                    var respHigh = PriceDeltaFromLine(item.Y1, item.Y2, barCourrent.High, item.Time1, item.Time2, barCourrent.OpenTime);
                    var respLow = PriceDeltaFromLine(item.Y1, item.Y2, barCourrent.Low, item.Time1, item.Time2, barCourrent.OpenTime);

                    Debug.WriteLine($"Trendline name {item.Comment} channel delta high: {respHigh} low: {respLow}");
                }
            }




            if (bars.Count < 3)
                return;

            var barPreLast = bars.TakeLast(3).First();
            var barLast = bars.TakeLast(2).First();
            double sdLastCandle = 0;
            //if (bars.Count > standardDeviationSampleNumber +1 && barLast.OpenTime == new DateTime(2024, 4, 19, 3, 0, 0))
            if (bars.Count > standardDeviationSampleNumber +1)
                {
                var candlesStandardDeviation = GetCandlesStandardDeviation(standardDeviationSampleNumber);
                sdLastCandle = GetStandardDeviationIfLastCandle(barLast, candlesStandardDeviation.sd, candlesStandardDeviation.mean);
                if (sdLastCandle > sdThreshold)
                {
                    Debug.WriteLine($"ZR: {barLast.OpenTime.ToString()} smallsigma SD: {candlesStandardDeviation.sd.ToString()}  ---- mean: {candlesStandardDeviation.mean.ToString()} --- sdLastCandle: {sdLastCandle}");
                }

                double GetStandardDeviationIfLastCandle(Bar barLast, double standardDevaition, double mean)
                {
                    var deltasHighLowLastCandle = Math.Abs(barLast.High - barLast.Low);

                    var resp = Math.Abs((mean - deltasHighLowLastCandle)) / standardDevaition;

                    return resp;
                }
            }


            //Plot on indicator chart (candle SD value) 
            if (sdLastCandle > sdThreshold)
            {
                //case pre candle is red and last candle is green
                if (barPreLast.Open > barPreLast.Close && barLast.Open < barLast.Close)
                {
                    //double sizeWickTop = barPreLast.High - barPreLast.Open;
                    //double sizeWickBottom = barPreLast.Close - barPreLast.Low;
                    //double sizeBody = barPreLast.Open - barPreLast.Close;
                    //if(((Math.Abs(sizeBody) / (Math.Abs(sizeWickTop) + Math.Abs(sizeWickBottom))) > (bodyPercentVsWick/100)) == false)
                    //{
                    //    Result[index - 1] = 0;
                    //    return;
                    //}

                    double tolleranceInDecimal = tollerancePercent / 100;
                    //if (barLast.Close*(1+ tolleranceInDecimal) > barPreLast.Open && barLast.Open * (1 - tolleranceInDecimal) <= barPreLast.Close)
                    if (Math.Abs(barLast.Open - barLast.Close) > Math.Abs(barPreLast.Open - barPreLast.Close))
                    {
                        //candle is Engulfed by Buyers
                        var engulfingRation = Math.Abs(barLast.Open - barLast.Close) / Math.Abs(barPreLast.Open - barPreLast.Close);
                        Result[index - 1] = sdLastCandle;
                        return;
                    }
                }
                //case pre candle is green and last candle is red
                else if (barPreLast.Open < barPreLast.Close && barLast.Open > barLast.Close)
                {
                    double tolleranceInDecimal = tollerancePercent / 100;
                    if (Math.Abs(barLast.Open - barLast.Close) > Math.Abs(barPreLast.Open - barPreLast.Close))
                    //if (barLast.Close * (1 - tolleranceInDecimal) < barPreLast.Open && barLast.Open * (1 + tolleranceInDecimal) >= barPreLast.Close)
                    {
                        //candle is Engulfed by Sellers
                        var engulfingRation = Math.Abs(barLast.Open - barLast.Close) / Math.Abs(barPreLast.Open - barPreLast.Close);
                        Result[index - 1] = -sdLastCandle;
                        return;
                    }
                }
            }
            else
            {
                Result[index - 1] = 0;
            }

        }


        private (double sd, double mean) GetCandlesStandardDeviation(int countCandles)
        {
            {
                var deltaHighLowOfCandles = GetDeltaHighLowOfCandles(countCandles);


                var resp_sd = SampleStandardDeviation(deltaHighLowOfCandles);
                var resp_mean = deltaHighLowOfCandles.Average();

                return (resp_sd, resp_mean);
            }

            List<double> GetDeltaHighLowOfCandles(int numberOfCandles)
            {
                var resp = new List<double>();

                var bars = MarketData.GetBars(this.TimeFrame);

                if (bars.Count == 0)
                    throw new Exception("ZR: there are 0 candles, high low delta cannot be evaluated ");

                if ((numberOfCandles + 1 < bars.Count) == false)
                {
                    return resp;
                }

                foreach (var item in bars.TakeLast(numberOfCandles))
                {
                    resp.Add(Math.Abs(item.High - item.Low));
                }

                //for (int i = 1; i <= numberOfCandles; i++)
                //{
                //    //var barTemp = bars.TakeLast(i + 1).First();
                //    var barTemp = bars.TakeLast(i).First();
                //    resp.Add(Math.Abs(barTemp.High - barTemp.Low));
                //}

                return resp;
            }

            double StandardDeviation(List<double> numberSet, double divisor)
            {
                double mean = numberSet.Average();
                return Math.Sqrt(numberSet.Sum(x => Math.Pow(x - mean, 2)) / divisor);
            }

            double PopulationStandardDeviation(List<double> numberSet)
            {
                var resp = StandardDeviation(numberSet, numberSet.Count);
                return resp;
            }

            double SampleStandardDeviation(List<double> numberSet)
            {
                var resp = StandardDeviation(numberSet, numberSet.Count - 1);
                return resp;
            }
        }

        private double? PriceDeltaFromLine(double priceY1, double priceY2, double priceNow, DateTime dt1, DateTime dt2, DateTime dtNow)
        {
            if (dtNow < dt1 && dt2 < dtNow)
                return null;

            //find angular coefficient m
            var m = (priceY2 - priceY1) / (dt2.Ticks - dt1.Ticks);
            var c = priceY1 - (m * dt1.Ticks);

            double priceYOnLine = (m * dtNow.Ticks) + c;

            double deltaDistance = priceNow - priceYOnLine;

            return deltaDistance;
        }
    }
}