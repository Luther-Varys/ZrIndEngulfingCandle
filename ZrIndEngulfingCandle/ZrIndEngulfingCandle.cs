using System;
using System.Collections.Generic;
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


        double tollerancePercent = 0;
        double bodyPercentVsWick = 200;
        public override void Calculate(int index)
        {
            var bars = MarketData.GetBars(this.TimeFrame);

            if (bars.Count < 3)
                return;

            var barPreLast = bars.TakeLast(3).First();
            var barLast = bars.TakeLast(2).First();

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
                    Result[index - 1] = engulfingRation;
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
                    Result[index - 1] = -engulfingRation;
                    return;
                }
            }

            Result[index - 1] = 0;
        }
    }
}