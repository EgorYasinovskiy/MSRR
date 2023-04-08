using System.Security.Cryptography.X509Certificates;

namespace MSRR2
{
	public class Experiment
	{
		public Dictionary<int, double[]> MeanBufferSizeByIntensityAndUserCount { get; set; } = new Dictionary<int, double[]>();

		private const int SLOTS = 100_000;
		private const double TRB = 5d / 10000d; // 0.5 мс
		private readonly Random _rand = new Random();

		private readonly object _lockResult = new object();
		private readonly object _lockBuffers = new object();

		public void DoExperiment(int abCount, Network network)
		{
			long[] buffers = RefreshBuffer(abCount);
			var bufferSum = new int[SLOTS - 1];
			var cqi = PrecomputeCQI(network);
			var intensities = Enumerable.Range(1, 100);
			var mean = new double[100];
			foreach (var intens in intensities)
			{
				var propability = 1d / ((intens * TRB) + 1);
				DownloadToBS(propability, buffers);
				var startUnit = 0;
				Parallel.For(1, SLOTS, x =>
				{
					var unitIndex = (int)((x - 1) % abCount);
					UploadFromBs(cqi[unitIndex][x - 1], unitIndex, buffers);
					bufferSum[x - 1] = buffers.Sum(x => Convert.ToInt32((x / 8 / 1024)));
					DownloadToBS(propability, buffers);
				});
				//for (int x = 1; x < SLOTS; x++)
				//{
				//	var unitIndex = (int)((x - 1) % abCount);
				//	UploadFromBs(cqi[unitIndex][x - 1], unitIndex, buffers);
				//	bufferSum[x - 1] = buffers.Sum(x => Convert.ToInt32((x / 8 / 1024)));
				//	DownloadToBS(propability, buffers);
				//}
				mean[intens - 1] = bufferSum.Average();
			}
			lock (_lockResult)
			{
				MeanBufferSizeByIntensityAndUserCount.Add(abCount, mean);
			}
		}

		private double[][] PrecomputeCQI(Network network)
		{
			var result = new double[network.Units.Count][];
			Parallel.For(0, network.Units.Count, unitId =>
			{
				var unit = network.Units[unitId];
				var lossValues = Enumerable.Range(1, SLOTS - 1).Select(x => 0d).ToArray();
				for (int i = 0; i < lossValues.Length; i++)
				{
					var norm = Normal(0, 1);
					var preval = unit.Loss + norm;
					var val = Math.Pow(10, preval / 10);
					lossValues[i] = val;
				}
				//.Select(x => unit.Loss + Normal(0, 1))
				//.ToArray();
				result[unitId] = lossValues.Select(loss => network.GetCQI(loss, unit.HeatLoss) * TRB).ToArray();
			});
			//for (int unitId = 0; unitId < network.Units.Count; unitId++)
			//{
			//	var unit = network.Units[unitId];
			//	var lossValues = Enumerable.Range(1, SLOTS - 1).Select(x => 0d).ToArray();
			//	for(int i = 0; i < lossValues.Length; i++)
			//	{
			//		var norm = Normal(0, 1);
			//		var preval = unit.Loss + norm;
			//		var val = Math.Pow(10, preval / 10);
			//		lossValues[i] = val;
			//	}
			//	var cqiList = new List<double>();
			//	foreach(var loss in lossValues)
			//	{
			//		cqiList.Add((double)network.GetCQI(loss, unit.HeatLoss) * TRB);
			//	}
			//	result[unitId] = cqiList.ToArray();
			//}
			return result;
		}

		public long[] RefreshBuffer(int abCount)
		{
			return Enumerable.Range(0, abCount).Select(x => 0L).ToArray();
		}

		public void DownloadToBS(double p, long[] buffers)
		{
			lock (_lockBuffers)
			{
				for (var i = 0; i < buffers.Length; i++)
					buffers[i] += Geometric(p) * 8 * 1024;
			}
		}

		public void UploadFromBs(double cqi, int unitIndex, long[] buffers)
		{
			var packetCount = (int)(cqi / 8 / 1024);
			var dataSize = packetCount * 8 * 1024;
			buffers[unitIndex] -= dataSize > buffers[unitIndex] ? buffers[unitIndex] : dataSize;
		}

		public int Geometric(double p)
		{
			// формула отсюда
			// https://math.stackexchange.com/questions/485448/prove-the-way-to-generate-geometrically-distributed-random-numbers

			double u = Random.Shared.NextDouble();
			return (int)Math.Ceiling(Math.Log(1 - u) / Math.Log(1 - p)) - 1;
		}

		public double Normal(double mean, double deviation)
		{
			// первый вариант, долгий но зато покрывает больше значений
			// https://ru.wikipedia.org/wiki/Преобразование_Бокса_—_Мюллера
			var r = Random.Shared.NextDouble();
			var fi = Random.Shared.NextDouble();
			var lnr = Math.Sqrt(-2d * Math.Log(r));
			var fiAngle = 2d * Math.PI * fi;
			var answers = new[] { lnr * Math.Cos(fiAngle), lnr * Math.Sin(fiAngle) };
			var val = mean + deviation * answers[_rand.Next(2)];
			return val;
		}
	}
}

