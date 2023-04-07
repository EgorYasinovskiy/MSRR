namespace MSRR
{
	public class Network
	{
		public BaseStation BaseStation { get; set; } = new BaseStation { Band = 20000000, Frequency = 2400000000, Power = 0.01 };
		public List<NetworkUnit> NetworkUnits { get; set; }
		public double Temperature { get; set; } = 300;
		public readonly double Bolcman = (1.38d * Math.Pow(10, -23));
		private List<double> CQIList { get => NetworkUnits.Select(GetCQI).ToList(); }


		public ExperimentResult Experiment(int n, Random rnd)
		{
			var propFair = Enumerable.Range(0, 100).Select(x => { Init(n,rnd); return ProportionFair(CQIList); });
			var maxThr = Enumerable.Range(0, 100).Select(x => { Init(n, rnd); return MaximumThroughput(CQIList); });
			var eqBlind = Enumerable.Range(0, 100).Select(x => { Init(n, rnd); return EqualBlind(CQIList); });
			var res = new Dictionary<string, NetworkSpecs>();

			return new ExperimentResult
			{
				ProportionFair = new NetworkSpecs()
				{
					MeanSpeed = propFair.Sum(x => x.MeanSpeed/10000000) / propFair.Count(),
					MinSpeed = propFair.Sum(x => x.MinSpeed / 10000000) / propFair.Count(),
					SumSpeed = propFair.Sum(x => x.SumSpeed / 10000000) / propFair.Count()
				},
				MaximumThroughput = new NetworkSpecs()
				{
					MeanSpeed = maxThr.Sum(x => x.MeanSpeed / 10000000) / maxThr.Count(),
					MinSpeed = maxThr.Sum(x => x.MinSpeed / 10000000) / maxThr.Count(),
					SumSpeed = maxThr.Sum(x => x.SumSpeed / 10000000) / maxThr.Count()
				},

				EqualBlind = new NetworkSpecs()
				{
					MeanSpeed = eqBlind.Sum(x => x.MeanSpeed / 10000000) / eqBlind.Count(),
					MinSpeed = eqBlind.Sum(x => x.MinSpeed / 10000000) / eqBlind.Count(),
					SumSpeed = eqBlind.Sum(x => x.SumSpeed / 10000000) / eqBlind.Count()
				}
			};
		}
		private NetworkSpecs ProportionFair(List<double> bandwidthList)
		{
			var proportion = 1d / NetworkUnits.Count;
			return new NetworkSpecs
			{
				MeanSpeed = bandwidthList.Sum() * proportion / bandwidthList.Count,
				SumSpeed = bandwidthList.Sum()  * proportion,
				MinSpeed = bandwidthList.Min() * proportion
			};
		}
		private NetworkSpecs MaximumThroughput(List<double> bandwidthList)
		{
			var max = bandwidthList.Max();
			return new NetworkSpecs
			{
				MeanSpeed = max / bandwidthList.Count,
				SumSpeed = max,
				MinSpeed = 0,
			};
		}
		private NetworkSpecs EqualBlind(List<double> bandwidthList)
		{
			var invertedSum = 1 / bandwidthList.Select(x => 1 / x).Sum();
			var coefList = bandwidthList.Select(x => 1 / x * invertedSum).ToList();
			var speed = bandwidthList[0] * coefList[0];

			return new NetworkSpecs
			{
				MeanSpeed = speed,
				SumSpeed = speed * bandwidthList.Count,
				MinSpeed = speed
			};
		}
		//Функция создания и размещения абонентов вокруг БС
		public void Init(int n, Random rnd)
		{
			NetworkUnits = new List<NetworkUnit>();
			for (var i = 0; i < n; i++)
			{
				NetworkUnits.Add(new NetworkUnit()
				{
					HeatLoss = 3,
					Position = new Position
					{
						Angle = rnd.Next(0, 361),
						Distance = (float)(rnd.NextDouble() * 10f)
					}
				});
			}
		}

		// Расчеты параметров
		private double GetCQI(NetworkUnit unit)
		{
			return BaseStation.Band * Math.Log2(1 + GetSNR(unit));
		}
		private double GetSNR(NetworkUnit unit)
		{
			return GetPRX(unit) / GetPN(unit);
		}
		private double GetPN(NetworkUnit unit)
		{
			return BaseStation.Band * Temperature * Bolcman * unit.HeatLoss;
		}
		private double GetPRX(NetworkUnit unit)
		{
			return BaseStation.Power / GetITU(unit);
		}
		private double GetITU(NetworkUnit unit)
		{
			var log = (20 * Math.Log10(BaseStation.Frequency / 1000000) + 29 * Math.Log10(unit.Position.Distance) - 28);
			var value = Math.Pow(10, log / 10);
			return value;
		}
	}
}
