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
    [Route("api/group")]
    public class GroupController : SnaBaseController
    {
        public GroupController(
            ApplicationDbContext context,
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IAmazonSimpleEmailService client

            ) : base(context, configuration, roleManager, client, userManager, signInManager)
        {

        }

        [HttpGet("allgroups")]
        public async Task<IActionResult> allgroups()
        {
            try
            {
                var users = _context.Group.Select(d => new
                {
                    d.Id,
                    d.DieticianId,
                    d.Name,
                    Users = d.UserProfile.Where(u => u.GroupId == d.Id).Select(x => new
                    {
                        x.Id,
                        x.FirstName
                    }).ToList()

                }).ToList();
                return Ok(JsonConvert.SerializeObject(users));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpGet]
        [Route("getgroup")]
        public async Task<IActionResult> getgroup([FromQuery]string id)
        {
            try
            {
                var user = _context.UserProfile.Select(d => new
                {
                    d.CreatedDate,
                    d.Id,
                    d.FirstName,
                    d.LastName,
                    d.Email,
                    d.PhoneNumber,
                }).Where(q => q.Id.ToString().Equals(id)).FirstOrDefault();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(user));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("getgroupmembers")]
        public async Task<IActionResult> getgroupmembers([FromQuery]string groupId)
        {
            try
            {
                var users = _context.UserProfile.Select(d => new
                {
                    d.GroupId,
                    d.FirstName,
                    d.LastName
                }).Where(q => q.GroupId.ToString().Equals(groupId)).ToList();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(users));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("removegroupmember")]
        public async Task<IActionResult> removegroupmember([FromQuery]UserProfile userobj)
        {
            try
            {
                var user = _context.UserProfile.Where(r => r.GroupId.Equals(userobj.GroupId)).FirstOrDefault();

                user.GroupId = null;
                _context.Update(user);

                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

            return StatusCode(StatusCodes.Status200OK, JsonConvert.SerializeObject(new returnMsg { message = "Patient removed from group." }));
        }

        [HttpGet]
        [Route("getdietitiangroups")]
        public async Task<IActionResult> getdietitiangroups()
        {
            try
            {
                var dietitianGroups = _context.UserProfile.Join(
                    _context.Group,
                    u => u.Id,
                    g => g.DieticianId,
                    (u, g) => new
                    {
                        dietitianName = u.FirstName + " " + u.LastName,
                        group = g.Id
                    });

                return Ok(JsonConvert.SerializeObject(dietitianGroups));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // get a list of patients for a group, containing their FirstName, 
        // LastName, Email, and time since their last UserFeedback post
        // Format of time?
        /**/
        [HttpGet]
        [Route("getGroupPatients")]
        public async Task<IActionResult> getGroupPatients([FromQuery] string id)
        {
            try
            {
                var users = _context.UserProfile.Select(d => new
                {
                    d.GroupId,
                    d.FirstName,
                    d.LastName,
                    d.Email,
                    userId = _context.UserProfile.Where(u => u.Id.ToString().Equals(id))
                                          .Select(x => x.Id)
                                          .FirstOrDefault(),
                    timeSinceLastPost = _context.UserFeedback.Select(x => new
                    {
                        // timeSinceLastPost is in days.hours:hours:minutes TimeSpan format
                        timesince = (TimeSpan)DateTime.Now.Subtract(x.Timestamp),
                        x.Rating

                    }).Where(y => y.UserId.ToString().Equals(userId))

                }).Where(q => q.GroupId.ToString().Equals(groupId)).ToList();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(users));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("test")]
        public async Task<IActionResult> test()
        {
            try
            {
                var users = _context.UserFeedback.Select(d => new
                {
                    // timesince is in days.hours:hours:minutes
                    timesince = (TimeSpan)DateTime.Now.Subtract(d.Timestamp),
                    //timesince2 = DateTime.timesince.ToString("")
                    d.Rating

                });
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(users));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
