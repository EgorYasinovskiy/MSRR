using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using OxyPlot;
using MSRR;
using OxyPlot.Series;
using Microsoft.Win32;
using System.Diagnostics;
using OxyPlot.Legends;

namespace Graph
{
	public partial class Form1 : Form
	{
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
			var poses = new Dictionary<int, List<Position>>();
			var results = new Dictionary<int, ExperimentResult>();
			var network = new Network();
			var rnd = new Random();
			for (int i = 1; i <= 64; i *= 2)
			{
				results.Add(i, network.Experiment(i, rnd));
				poses.Add(i, network.NetworkUnits.Select(x => x.Position).ToList());
			}


			// Построение моделей для графиков
			var posModels = poses.Select(x =>
			{
				var model = new PlotModel();
				model.Title = $"График расположения {x.Key} абонентов.";
				model.Axes.Add(new AngleAxis { Minimum = 0, Maximum = 360, MajorStep = 60, MinorStep = 15, Title = "Угол" });
				model.Axes.Add(new MagnitudeAxis { Minimum = 0, Maximum = 10, MajorStep = 2, MinorStep = 0.5, Title = "Расстояние" });
				var series = new OxyPlot.Series.ScatterSeries();
				var points = x.Value.Select(pos => new ScatterPoint(pos.Distance/100, pos.Angle)).ToArray();
				series.Points.AddRange(points);
				Trace.WriteLine(string.Join(',', points.Select(x => x.X.ToString() + " " + x.Y.ToString()).ToArray()));
				series.MarkerSize = 6;
				series.MarkerType = MarkerType.Circle;
				series.MarkerFill = OxyColors.Red;
				model.IsLegendVisible = true;
				model.Series.Add(series);
				model.PlotType = PlotType.Polar;
				return model;
			}).ToList();

			var resModels = typeof(ExperimentResult).GetProperties().Select(propInfo =>
			{
				var expModel = new PlotModel();
				expModel.Axes.Add(new LinearAxis() { Title = "Математическое ожидание (МБ)" });
				expModel.Axes.Add(new LinearAxis() { Title = "Количество абонентов", Position = AxisPosition.Bottom });
				expModel.Title = $"Данные эксперимента для алгоритма {propInfo.Name}";
				expModel.Series.Add(new LineSeries()
				{
					ItemsSource = results.Select(x => new DataPoint(x.Key, ((NetworkSpecs)propInfo.GetValue(x.Value)).SumSpeed)),
					StrokeThickness = 2,
					Title = "Суммарная скорость",
					Color = OxyColor.FromRgb(255, 0, 0)
				});
				expModel.Series.Add(new LineSeries()
				{
					ItemsSource = results.Select(x => new DataPoint(x.Key, ((NetworkSpecs)propInfo.GetValue(x.Value)).MeanSpeed)),
					Title = "Средняя скорость",
					StrokeThickness = 2,
					Color = OxyColor.FromRgb(0, 255, 0)

				});
				expModel.Series.Add(new LineSeries()
				{
					ItemsSource = results.Select(x => new DataPoint(x.Key, ((NetworkSpecs)propInfo.GetValue(x.Value)).MinSpeed)),
					StrokeThickness = 2,
					Title = "Минимальная скорость",
					Color = OxyColor.FromRgb(0, 0, 255)
				});
				expModel.IsLegendVisible = true;
				expModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.RightTop});

				return expModel;
			}).ToList();

			_models.Clear();
			_models.AddRange(posModels);
			_models.AddRange(resModels);
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

		private void RefreshData()
		{
			button1.Enabled = _selectedModel != 0;
			button2.Enabled = _selectedModel != (_models.Count - 1);

			plotView1.Model = _models[_selectedModel];
			Update();
		}
	}
}