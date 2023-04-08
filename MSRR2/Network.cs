using System.Runtime.ExceptionServices;

namespace MSRR2
{
	public class Network
	{

		//2|Окумура-Хата, large city|3000|160|1800|2
		const int Temperature = 300;
		const int LargeCityCoef = 3;
		const int UnitHeatLoss = 2;
		const int Radius = 3000;

		readonly double Bolcman;
		readonly double HRxLg;
		readonly double HBSLg;
		readonly double FreqLg;
		readonly double aHRx;
		readonly double HBS = 30;
		readonly double HRx = 2;

		public BaseStation BaseStation { get; set; }
		public List<NetworkUnit> Units { get; set; }

		public Network()
		{
			BaseStation = new BaseStation() { Band = 180000, Frequency = 1800000000, Power = 160 };
			FreqLg = Math.Log10(BaseStation.Frequency);
			aHRx = (1.1d * FreqLg - 0.7d) * HRx - (1.56d * FreqLg - 0.8d);
			HRxLg = Math.Log10(HRx);
			HBSLg = Math.Log10(HBS);
			Bolcman = (1.38d * Math.Pow(10, -23));
		}

		public void ResetUnits(int unitCount)
		{
			Units = new List<NetworkUnit>();
			var rnd = new Random();
			for (int i = 0; i < unitCount; i++)
			{
				var unit = new NetworkUnit()
				{
					HeatLoss = UnitHeatLoss,
					Position = new Position()
					{
						Angle = rnd.Next(0, 360),
						Distance = rnd.Next(Radius + 1)
					}
				};
				ComputeOkumuraLoss(unit);
				Units.Add(unit);
			}
		}

		public double GetCQI(double loss, double heatLoss)
		{
			var snr = GetSNR(loss, heatLoss);
			//var log2 = Math.Log2(1 + snr); // Это говно возвращает 0 для маленьких значений. Нужно переписать используя меньший эпсилон (-производительнось)
			var log = LogN((decimal)snr, 2m);
			var val = BaseStation.Band * log;
			return (double)val;
			//return BaseStation.Band * Math.Log2(1 + GetSNR(loss, heatLoss));
		}
		private double GetSNR(double loss, double heatLoss)
		{
			var prx = GetPRX(loss);
			var pn = GetPN(heatLoss);
			var val = prx / pn;
			return val;
			return GetPRX(loss) / GetPN(heatLoss);
		}
		private double GetPN(double heatLoss)
		{
			var val = BaseStation.Band * Temperature * Bolcman * heatLoss;
			return val;
			return BaseStation.Band * Temperature * Bolcman * heatLoss;
		}
		private double GetPRX(double loss)
		{
			var val = BaseStation.Power / loss;
			return val;
			return BaseStation.Power / loss;
		}
		public void ComputeOkumuraLoss(NetworkUnit unit)
		{
			var ldb = 46.3 + 33.9 * FreqLg - 13.82 * HBSLg - aHRx + (44.9 - 6.55 * HRxLg) * Math.Log10(unit.Position.Distance / 1000f) + LargeCityCoef;
			unit.Loss = ldb;
		}
		

		// Удачная ссылочка, в нашем случае как раз нужно 1+SNR, что можно разложить в такой логарифмический ряд
		// https://www.math10.com/ru/algebra/logarifmi-log-lg-ln/logarifmi.html
		public static decimal Log(decimal x, decimal e)
		{
			decimal result = 0;
			decimal prevRes = decimal.MaxValue;
			decimal pow = 1;
			while(Math.Abs(prevRes-result) > e)
			{
				prevRes = result;
				decimal poweredX = 1;
				for (int i = 0; i < pow; i++)
				{
					poweredX*= x;
				}

				decimal tmp = pow % 2 == 0 ? -poweredX: poweredX;
				result += tmp / pow;
			}

			return result;
		}

		public static decimal LogN(decimal x, decimal @base, decimal e = 1e-6m)
		{

			//log_a(b) = log_c(b) / log_c(a); В нашем случае с = e(2.71)
			decimal ln_x = Log(x, e);
			// Оптимизация популярных основний
			decimal ln_a = 0m;
			switch (@base)
			{
				case 2m:
					ln_a = 0.69314718056m;
					break;
				case 10m:
					ln_a = 2.30258509299m;
					break;
				case (decimal)Math.E:
					ln_a = 1;
					break;
				default:
					ln_a = Log(@base, e);
					break;

			}
			decimal result = ln_x / ln_a;

			return result;
		}
	}
}
