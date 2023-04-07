using System.Diagnostics;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace MSRR2.Graph
{
	public partial class Form1 : Form
	{
		private int[] _abonentsCounts { get; set; } = { 2, 4, 8, 16, 32, 64};
		private object _lock = new object();
		private Dictionary<int, OxyColor> _colors = new Dictionary<int, OxyColor>(3)
		{
			{ 2, OxyColors.OrangeRed},
			{ 4, OxyColors.Purple},
			{ 8, OxyColors.Coral},
			{ 16, OxyColors.Orange},
			{ 32, OxyColors.Green},
			{ 64, OxyColors.Blue},
		};
		private List<PlotModel> _models = new List<PlotModel>();
		private int _selectedModel = 0;

		public Form1()
		{
			InitializeComponent();
			Experiment();
			RefreshData();
		}

		public void Experiment()
		{
			Experiment experiment = new Experiment();
			foreach(var abCount in _abonentsCounts)
			{
				Network network = new Network();
				network.ResetUnits(abCount);
				GeneratePosModel(network);
				experiment.DoExperiment(abCount, network);
			}

			GenerateResultModel(experiment);
		}

		private void GeneratePosModel(Network network)
		{
			var model = new PlotModel();
			model.Title = $"График расположения {network.Units.Count} абонентов.";
			model.Axes.Add(new AngleAxis { Minimum = 0, Maximum = 360, MajorStep = 60, MinorStep = 15, Title = "Угол" });
			model.Axes.Add(new MagnitudeAxis { Minimum = 0, Maximum = 3, MajorStep = 1, MinorStep = 0.3, Title = "Расстояние (Км)" });
			var series = new OxyPlot.Series.ScatterSeries();
			var points = network.Units.Select(x => new ScatterPoint(x.Position.Distance/1000d, x.Position.Angle)).ToArray();
			series.Points.AddRange(points);
			Trace.WriteLine(string.Join(',', points.Select(x => x.X.ToString() + " " + x.Y.ToString()).ToArray()));
			series.MarkerSize = 6;
			series.MarkerType = MarkerType.Circle;
			series.MarkerFill = OxyColors.Red;
			model.Series.Add(series);
			model.IsLegendVisible = true;
			model.PlotType = PlotType.Polar;

			_models.Add(model);
		}

		private void GenerateResultModel(Experiment experiment)
		{
			var expModel = new PlotModel();
			expModel.Axes.Add(new LinearAxis() { Title = "Объем буффера (Мб)" });
			expModel.Axes.Add(new LinearAxis() { Title = "Интенсивность потока (Пак/с)", Position = AxisPosition.Bottom });
			expModel.Title = $"Зависимость среднего суммарно объема данных находящихся в буфере у всех АБ от интенсивности входного потока";
			foreach (var res in experiment.MeanBufferSizeByIntensityAndUserCount) 
			{
				expModel.Series.Add(new LineSeries()
				{
					ItemsSource = res.Value.Select((value, index) => new DataPoint(index, value/1024)),
					StrokeThickness = 4,
					Title = $"{res.Key} абонентов в сети",
					Color = _colors[res.Key]
				});
			}
			//for (int i = 0; i < experiment.MeanBufferSizeByIntensityAndUserCount.Count; i++)
			//{
			//	expModel.Series.Add(new LineSeries()
			//	{
			//		ItemsSource = x.Value.Select((value, index) => new DataPoint(index, value)),
			//		StrokeThickness = 4,
			//		Title = $"{x.Key} абонентов в сети",
			//		Color = _colors[x.Key]
			//	});
			//}
			//experiment.MeanBufferSizeByIntensityAndUserCount.Select(x =>
			//{
			//	expModel.Series.Add(new LineSeries()
			//	{
			//		ItemsSource = x.Value.Select((value,index) => new DataPoint(index, value)),
			//		StrokeThickness = 4,
			//		Title = $"{x.Key} абонентов в сети",
			//		Color = _colors[x.Key]
			//	});
			//	return 0;
			//});
			expModel.IsLegendVisible = true;
			expModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.RightTop });
			_models.Add(expModel);
		}

		private void RefreshData()
		{
			button1.Enabled = _selectedModel != 0;
			button2.Enabled = _selectedModel != (_models.Count - 1);

			plotView1.Model = _models[_selectedModel];
			Update();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			_selectedModel--;
			RefreshData();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			_selectedModel++;
			RefreshData();
		}
	}
}