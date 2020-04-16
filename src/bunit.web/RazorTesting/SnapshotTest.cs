using System;
using System.Threading.Tasks;
using Bunit.Diffing;
using Bunit.Mocking.JSInterop;
using Bunit.RazorTesting;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bunit
{
	/// <summary>
	/// A component used to create snapshot tests.
	/// Snapshot tests takes two child inputs, a TestInput section and a ExpectedOutput section.
	/// It then compares the result of rendering both using semantic HTML comparison.
	/// </summary>
	public class SnapshotTest : RazorTest
	{
		/// <summary>
		/// Sets the setup action to perform before the <see cref="TestInput"/> and <see cref="ExpectedOutput"/>
		/// is rendered and compared.
		/// </summary>
		[Parameter] public Action<SnapshotTest>? Setup { private get; set; }

		/// <summary>
		/// Sets the setup action to perform before the <see cref="TestInput"/> and <see cref="ExpectedOutput"/>
		/// is rendered and compared.
		/// </summary>
		[Parameter] public Func<SnapshotTest, Task>? SetupAsync { private get; set; }

		/// <summary>
		/// Gets or sets the input to the snapshot test.
		/// </summary>
		[Parameter] public RenderFragment TestInput { private get; set; } = default!;

		/// <summary>
		/// Gets or sets the expected output of the snapshot test.
		/// </summary>
		[Parameter] public RenderFragment ExpectedOutput { private get; set; } = default!;

		/// <inheritdoc/>
		protected override async Task Run()
		{
			Services.AddSingleton<IJSRuntime>(new PlaceholderJsRuntime());

			if (Setup is { })
				TryRun(Setup, this);
			if (SetupAsync is { })
				await TryRunAsync(SetupAsync, this).ConfigureAwait(false);

			var testRenderId = await Renderer.RenderFragment(TestInput).ConfigureAwait(false);
			var expectedRenderId = await Renderer.RenderFragment(ExpectedOutput).ConfigureAwait(false);

			var inputHtml = Htmlizer.GetHtml(Renderer, testRenderId);
			var expectedHtml = Htmlizer.GetHtml(Renderer, expectedRenderId);

			var parser = new TestHtmlParser();
			var inputNodes = parser.Parse(inputHtml);
			var expectedNodes = parser.Parse(expectedHtml);

			var diffs = inputNodes.CompareTo(expectedNodes);

			if (diffs.Count > 0)
				throw new HtmlEqualException(diffs, expectedNodes, inputNodes, Description);
		}

		/// <inheritdoc/>
		public override Task SetParametersAsync(ParameterView parameters)
		{
			Setup = parameters.GetValueOrDefault<Action<SnapshotTest>>(nameof(Setup));
			SetupAsync = parameters.GetValueOrDefault<Func<SnapshotTest, Task>>(nameof(SetupAsync));

			if (parameters.TryGetValue<RenderFragment>(nameof(TestInput), out var input))
				TestInput = input;
			else
				throw new InvalidOperationException($"No {nameof(TestInput)} specified in the {nameof(SnapshotTest)} component.");

			if (parameters.TryGetValue<RenderFragment>(nameof(ExpectedOutput), out var output))
				ExpectedOutput = output;
			else
				throw new InvalidOperationException($"No {nameof(ExpectedOutput)} specified in the {nameof(SnapshotTest)} component.");

			return base.SetParametersAsync(parameters);
		}
	}
}