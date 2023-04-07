﻿namespace MSRR2
{
	public class Experiment
	{
		public Dictionary<int, double[]> MeanBufferSizeByIntensityAndUserCount { get; set; } = new Dictionary<int, double[]>();

		private const int SLOTS = 1_000_00;
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
				Parallel.For(1, SLOTS, new ParallelOptions { MaxDegreeOfParallelism = 12 }, x =>
				{
					var unitIndex = (int)((x - 1) % abCount);
					UploadFromBs(cqi[unitIndex][x - 1], unitIndex, buffers);
					DownloadToBS(propability, buffers);
					bufferSum[x - 1] = buffers.Sum(x => Convert.ToInt32((x / 8 / 1024)));
				});
				mean[intens-1] = bufferSum.Average();
			}
			lock (_lockResult)
			{
				MeanBufferSizeByIntensityAndUserCount.Add(abCount, mean);
			}
		}

		private double[][] PrecomputeCQI(Network network)
		{
			var result = new double[network.Units.Count][];
			Parallel.For(0, network.Units.Count, new ParallelOptions { MaxDegreeOfParallelism = 12 }, unitId =>
			{
				var unit = network.Units[unitId];
				var lossValues = Enumerable.Range(1, SLOTS - 1)
					.Select(x => unit.Loss + Normal(0, 1))
					.ToArray();
				result[unitId] = lossValues.Select(loss => network.GetCQI(loss, unit.HeatLoss) * TRB).ToArray();
			});
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

			double u = _rand.NextDouble();
			return (int)Math.Ceiling(Math.Log(1 - u) / Math.Log(1 - p))-1;
		}

		public double Normal(double mean, double deviation)
		{
			double randNum = _rand.NextDouble();
			double normalRand = Math.Sqrt(-2 * Math.Log(randNum)) * Math.Cos(2 * Math.PI * _rand.NextDouble());
			normalRand = normalRand * deviation + mean;
			return normalRand;
		}
	}
}
