using Framework.Slides;
using Microsoft.AspNetCore.Components;
using Framework.Game.Parameters;
using GameStateInventory;


namespace Framework.Game;

public partial class GameBase : ComponentBase
{
	[Inject]
	protected SlideService SlideService { get; set; } = null!;

	[Inject]
	protected GameState GameState { get; set; } = null!;

	// // private readonly TaskCompletionSource<bool> _tcs = new();
	// // protected Task InitTask => _tcs.Task;

	protected string SlideId => Parameters.SlideId;

	protected SlideComponentParameters Parameters { get; set; } = null!;
	// protected Dictionary<string, object?> ParametersDictionary { get; set; } = null!;


	protected override void OnInitialized()
	{
		string slideId = SlideService.GetStartSlideId();
		Parameters = new SlideComponentParameters()
		{
			SlideId = slideId,
			OnButtonClick = EventCallback.Factory.Create<List<List<string>>>(this, EvaluateActions)
		};
		// // _tcs.SetResult(true);
	}

	protected static void HandlePolygonClick(string thing)
	{
		Console.WriteLine(thing);
	}

	protected void ChangeSlide(string slideId)
	{
		Parameters.SlideId = slideId;
		// Console.WriteLine(SlideId);
		StateHasChanged();
	}

	protected void FinishMinigame(bool success)
	{
		if (success)
		{
			ChangeSlide(SlideService.GetSlide(Parameters.SlideId).FallbackSlide!);
		}
		// TODO: Also, maybe make this function actually do something different based on success
		else
		{
			ChangeSlide(SlideService.GetSlide(Parameters.SlideId).FallbackSlide!);
		}
	}

	protected struct Block
	{
		public List<string> Stack { get; set; }
		public bool Skipping { get; set; }
		public string SkippingTo { get; set; }
	}

	protected async Task EvaluateActions(List<List<string>> actions)
	{
		Block block = new()
		{
			Stack = new()
		};
		foreach (List<string> action in actions)
		{
			if (block.Skipping)
			{
				if (block.SkippingTo == action[1] && action[0] == "EndBlock")
				{
					block.Skipping = false;
					// block.Stack.RemoveAt(block.Stack.Count - 1);
					// removed this cause it gets removed in the switch
					// block.Stack.Remove(block.Stack.Last());
					block.SkippingTo = "";
				}
				else
				{
					continue;
				}
			}

			switch (action[0])
			{
				case "Route":
					ChangeSlide(action[1]);
					break;
				case "AddItem":
					GameState.AddItem(action[1]);
					break;
				case "RemoveItem":
					GameState.RemoveItem(action[1]);
					break;
				case "SetGameState":
					switch (action[2])
					{
						case "true":
							GameState.SetVisibility(action[1], true);
							break;
						case "false":
							GameState.SetVisibility(action[1], false);
							break;
						case "toggle":
							GameState.ChangeVisibility(action[1]);
							break;
						default:
							break;
					}
					break;

				case "RequireItem":
					// if the check is negated
					if (action[1].StartsWith("!"))
					{
						// remove leading "!"
						if (!GameState.CheckForItem(action[1][1..]))
						{
							// if it is true, continue with executing
							// Console.WriteLine($"Required Item: {action[1]}");
							break;
						}
					}
					// if the check if not negated
					else
					{
						if (GameState.CheckForItem(action[1]))
						{
							// if it is true, continue with executing
							// Console.WriteLine($"Required Item: {action[1]}");
							break;
						}
					}
					// if the checks have been false

					// if there are blocks on the stack
					if (block.Stack.Count > 0)
					{
						// start skipping to corresponding EndBlock statement
						block.Skipping = true;
						block.SkippingTo = block.Stack.Last();
						break;
					}
					// if no block is on the stack, just exit entirely
					else
					{
						return;
					}

				case "RequireGameState":
					// if the check is negated
					if (action[1].StartsWith("!"))
					{
						// remove leading "!"
						if (!GameState.CheckVisibility(action[1][1..]))
						{
							// if it is true, continue with executing
							// Console.WriteLine($"Required GameState: {action[1]}");
							break;
						}
					}
					// if the check if not negated
					else
					{
						if (GameState.CheckVisibility(action[1]))
						{
							// if it is true, continue with executing
							// Console.WriteLine($"Required GameState: {action[1]}");
							break;
						}
					}
					// if the checks have been false

					// if there are blocks on the stack
					if (block.Stack.Count > 0)
					{
						// start skipping to corresponding EndBlock statement
						block.Skipping = true;
						block.SkippingTo = block.Stack.Last();
						// Console.WriteLine($"Skipping to {block.SkippingTo}");
						break;
					}
					// if no block is on the stack, just exit entirely
					else
					{
						// Console.WriteLine("return");
						return;
					}

				case "StartBlock":
					block.Stack.Add(action[1]);
					// Console.WriteLine($"StartBlock {action[1]}");
					break;
				case "EndBlock":
					block.Stack.Remove(block.Stack.Last());
					// Console.WriteLine($"EndBlock {action[1]}");
					break;

				case "Exit":
					// Console.WriteLine("return");
					return;

				default:
					break;
			}
		}
	}
}