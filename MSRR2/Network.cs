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
			aHRx = (1.1 * FreqLg - 0.7) * HRx - (1.56 * FreqLg - 0.8);
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
			return BaseStation.Band * Math.Log2(1 + GetSNR(loss, heatLoss));
		}
		private double GetSNR(double loss, double heatLoss)
		{
			return GetPRX(loss) / GetPN(heatLoss);
		}
		private double GetPN(double heatLoss)
		{
			return BaseStation.Band * Temperature * Bolcman * heatLoss;
		}
		private double GetPRX(double loss)
		{
			return BaseStation.Power / loss;
		}
		public void ComputeOkumuraLoss(NetworkUnit unit)
		{
			var ldb = 46.3 + 33.9 * FreqLg - 13.82 * HBSLg - aHRx + (44.9 - 6.55 * HRxLg) * Math.Log10(unit.Position.Distance / 1000f) + LargeCityCoef;
			unit.Loss = Math.Pow(10, ldb / 10);
		}
	}
}
