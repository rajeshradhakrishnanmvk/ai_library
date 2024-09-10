using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class LightsPlugin
{

   // Mock data for the lights
   private readonly List<LightModel> _lights = new()
   {
      new LightModel { Id = 1, Name = "Table Lamp", IsOn = false },
      new LightModel { Id = 2, Name = "Porch light", IsOn = false },
      new LightModel { Id = 3, Name = "Chandelier", IsOn = true }
   };

   [KernelFunction("get_lights")]
   [Description("Gets a list of lights and their current state")]
   [return: Description("An array of lights")]
   public async Task<List<LightModel>> GetLightsAsync()
   {
      return _lights;
   }

   [KernelFunction("change_state")]
   [Description("Changes the state of the light")]
   [return: Description("The updated state of the light; will return null if the light does not exist")]
   public async Task<LightModel?> ChangeStateAsync(LightModel changeState)
   {
      // Find the light to change
      var light = _lights.FirstOrDefault(l => l.Id == changeState.Id);

      // If the light does not exist, return null
      if (light == null)
      {
         return null;
      }

      // Update the light state
      light.IsOn = changeState.IsOn;
      //light.Brightness = changeState.Brightness;
      //light.Color = changeState.Color;

      return light;
   }
}