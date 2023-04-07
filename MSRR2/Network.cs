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

		public decimal GetCQI(decimal loss, double heatLoss)
		{
			var valLg = (decimal)Math.Log2((double)(1 + GetSNR(loss, heatLoss)));
			return BaseStation.Band * valLg;
		}
		private decimal GetSNR(decimal loss, double heatLoss)
		{
			var val = GetPRX(loss) / (decimal)GetPN(heatLoss);
			return val;
		}
		private double GetPN(double heatLoss)
		{
			var val = BaseStation.Band * Temperature * Bolcman * heatLoss;
			return val;
		}
		private decimal GetPRX(decimal loss)
		{
			var val = ((decimal)BaseStation.Power) / loss;
			return val;
		}
		public void ComputeOkumuraLoss(NetworkUnit unit)
		{
			var ldb = 46.3d + 33.9d * FreqLg - 13.82d * HBSLg - aHRx + (44.9d - 6.55d * HRxLg) * Math.Log10(unit.Position.Distance / 1000d) + LargeCityCoef;
			unit.Loss = ldb;
		}
	}
}
