using JsonUtilities;
using Framework.Slides.JsonClasses;
using GameStateInventory;

namespace Framework.Slides;

public class PositionPresets
{

}

public class SlideService
{
	private readonly JsonUtility jsonUtility;
	private readonly GameState gameState;

	public Dictionary<string, JsonSlide> Slides { get; private set; } = null!;

	public Dictionary<JsonSlide, string> InverseSlides { get; private set; } = null!;

	// private readonly TaskCompletionSource<bool> _initCompletionSource = new();
	// public Task Initialization => _initCompletionSource.Task;

	private async Task<Dictionary<string, JsonSlide>> FetchSlidesAsync(string url)
	{
		// assign return value from GetFromJsonAsync to slides if it is not null, otherwise throw an exception
		// var slides = await Http.GetFromJsonAsync<Dictionary<string, JsonSlide>>(url) ?? 
		// throw new Exception("Slides is null");
		var slides = await jsonUtility.LoadFromJsonAsync<Dictionary<string, JsonSlide>>(url);
		return slides;
	}

	public SlideService(JsonUtility jsonUtility, GameState gameState)
	{
		this.jsonUtility = jsonUtility;
		this.gameState = gameState;
		// Slides = FetchSlidesAsync("Slides.json").Result;
		// InverseSlides = Slides.ToDictionary(x => x.Value, x => x.Key);
	}

	public async Task Init()
	{
		Slides = await FetchSlidesAsync("Slides.json");
		InverseSlides = Slides.ToDictionary(x => x.Value, x => x.Key);
		// _initCompletionSource.SetResult(true);
		CreateGameStateEntries();
	}

	public JsonSlide GetSlide(string slideId)
	{
		try
		{
			return Slides[slideId];
		}
		catch (KeyNotFoundException)
		{
			throw new Exception($"No Slide with Id `{slideId}` found");
		};
	}

	public string GetSlideId(JsonSlide slide)
	{
		try
		{
			// Works, but is slow
			// return Slides.FirstOrDefault(x => x.Value == slide).Key;
			// use inverted dictionary
			return InverseSlides[slide];
		}
		catch (KeyNotFoundException)
		{
			//TODO: Implement ToString in JsonSlide
			throw new Exception("No Id corresponding to given JsonSlide found");
		}
	}

	// As of now, this is quite a useless function, but maybe we can add a flag 
	// to the Slides.json that makes a slide the first one no matter the order
	public string GetStartSlideId()
	{
		// Console.WriteLine(Slides);
		return Slides.Keys.First();
		// return "error";
	}

	// TODO: Add verify slides function

	public void CreateGameStateEntries()
	{
		// iterate over slides
		foreach (var slide in Slides)
		{
			// if slide is a minigame, skip it
			if (slide.Value.Type is string type)
			{
				if (type == "Minigame") { continue; }
			}
			foreach (var button in slide.Value.Buttons!)
			{
				if (button.Value.Visible is bool visible)
				{
					if (visible)
					{
						gameState.AddVisibility($"{slide.Key}.{button.Key}", true);
					}
					else
					{
						gameState.AddVisibility($"{slide.Key}.{button.Key}", false);
					}
				}
			}
		}
	}
}