using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SNAFramework.Data;
using SNAFramework.Models;

namespace SNAFramework.Controllers
{
    //Default security to only request with JWT Bearer Tokens
    //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Developer")]
    [Route("api/recipe")]
    public class RecipeController : SnaBaseController
    {
        public RecipeController(
            ApplicationDbContext context,
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IAmazonSimpleEmailService client

            ) : base(context, configuration, roleManager, client, userManager, signInManager)
        {

        }

        [HttpGet("allrecipes")]
        public async Task<IActionResult> allrecipes()
        {
            try
            {
                var recipes = _context.Recipe.Select(d => new
                {
                    //d.RecipeId,
                    d.Name,
                    d.Calories

                }).OrderBy(x => x.Name).ToList();
                return Ok(JsonConvert.SerializeObject(recipes));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpGet]
        [Route("getrecipe")]
        public async Task<IActionResult> getrecipe([FromQuery]string recipename)
        {
            try
            {
                var recipe = _context.Recipe.Select(d => new
                {
                    //d.RecipeId,
                    d.Name,
                    d.Calories

                }).Where(q => q.Name.Equals(recipename)).FirstOrDefault();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(recipe));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        [Route("addrecipe")]
        public async Task<IActionResult> addrecipe([FromBody]string recipeobj)
        {
            // Get recipeobj containing recipe info
            // Create a new entry in the Recipe table (model) with given info

            try
            {
               // Add recipe to the Recipe table (model)
               // Return a confirmation message

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        [Route("editrecipe")]
        public async Task<IActionResult> editrecipe([FromBody]string recipeobj)
        {

            try
            {
                // Find recipe the Recipe table (model)
                // Update the recipe with the new information
                // Save changes
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        [Route("removerecipe")]
        public async Task<IActionResult> removerecipe([FromBody]string recipeobj)
        {
            
            try
            {
                // Find recipe in the Recipe table (model)
                // Remove recipe
                // Return a confirmation message
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
