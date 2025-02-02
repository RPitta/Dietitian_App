﻿using System;
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
    [Route("api/user")]
    public class UserController : SnaBaseController
    {

        public UserController(
        ApplicationDbContext context,
        IConfiguration configuration,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IAmazonSimpleEmailService client
        ) : base(context, configuration, roleManager, client, userManager, signInManager)
        {

        }

        [HttpGet("allusers")]
        public async Task<IActionResult> allusers()
        {
            try
            {
                var users = _context.UserProfile.Select(d => new
                {
                    d.CreatedDate,
                    d.Id,
                    d.FirstName,
                    d.LastName,
                    d.Email,
                    d.PhoneNumber,
                    d.Inactive

                }).OrderBy(x => x.FirstName).ToList();
                return Ok(JsonConvert.SerializeObject(users));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpGet]
        [Route("getuser")]
        public async Task<IActionResult> getuser([FromQuery]string id)
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
                    d.GroupId,
                }).Where(q => q.Id.ToString().Equals(id)).FirstOrDefault();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(user));
            }
            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("lockupdate")]
        public async Task<IActionResult> lockupdate([FromQuery]int userid,bool operation)
        {
            try
            {
                var user = _context.UserProfile.Where(q => q.Id.Equals(userid)).FirstOrDefault();
                user.Inactive = operation;
                _context.Update(user);
                _context.SaveChanges();
                return StatusCode(StatusCodes.Status200OK, JsonConvert.SerializeObject(new returnMsg { message = user.Email + " Updated." }));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
        
        [HttpPost]
        [Route("createnewuser")]
        public async Task<IActionResult> CreateNewUser([FromBody]NewUser userobj)
        {
            var user = new IdentityUser { UserName = userobj.Email, Email = userobj.Email,  PhoneNumber = userobj.PhoneNumber};
            var result = await _userManager.CreateAsync(user, userobj.Password);
            if (result.Succeeded)
            {
                
                IdentityUser curUser = _context.Users.Where(y => y.Email == userobj.Email).FirstOrDefault();
                UserProfile newuser = new UserProfile();
                {
                    newuser.IdentityUserId = curUser.Id;
                    newuser.Email = curUser.Email;
                    newuser.Inactive = false;
                    newuser.PhoneNumber = userobj.PhoneNumber;
                    newuser.FirstName = userobj.FirstName;
                    newuser.LastName = userobj.LastName;
                    newuser.CreatedDate = DateTime.Today;
                }
                try
                {
                    _context.UserProfile.Add(newuser);
                    _context.SaveChanges();
                    IdentityUser createduser = _context.Users.Where(r => r.Email == curUser.Email).First();
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    _AuthMessageSender.SendEmailAsync(_appData.ApplicationName + " Account Created on "+ _appData.ApplicationName,"Your Account is created please use the link below confirm your email :  "
                    + _appData.PasswordResetLink + "?email=" + curUser.Email + "&token=" + code, _AuthMessageSender.SystemGeneralEmail, new List<string> { curUser.Email });

                    await _userManager.AddToRolesAsync(curUser, userobj.rolestring);
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, e.Message);

                }

                return StatusCode(StatusCodes.Status200OK, JsonConvert.SerializeObject(new returnMsg { message = userobj.Email + " User created." }));
            }
            else
            {
                var exceptionText = result.Errors.First();
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = exceptionText.Description }));
            }
        }

        [HttpPost]
        [Route("edituser")]
        public async Task<IActionResult> editusers([FromBody]UserProfile userobj)
        {
           
            try
            {
                if (userobj.Id == null)
                {
                    throw new Exception("wrong information");
                }
                UserProfile user = _context.UserProfile.Where(r => r.Id.Equals(userobj.Id)).FirstOrDefault();

                IdentityUser euser = _context.Users.Where(v => v.Email.Equals(user.Email)).First();
                await _userManager.SetUserNameAsync(euser, userobj.Email);
                string emailtoken = await _userManager.GenerateChangeEmailTokenAsync(euser, userobj.Email);
                await _userManager.ChangeEmailAsync(euser, userobj.Email, emailtoken);

                user.FirstName = userobj.FirstName;
                user.LastName = userobj.LastName;
                user.Email = userobj.Email;
                user.PhoneNumber = userobj.PhoneNumber;
                user.LastModificationDate = DateTime.Today;
                _context.Update(user);

                _context.SaveChanges();
                return StatusCode(StatusCodes.Status200OK, JsonConvert.SerializeObject(new returnMsg { message = userobj.Email + " profile updated." }));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "User Not Updated");
            }

        }

        [HttpPost]
        [Route("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody]changepassword userobj)
        {
            try
            {

                IdentityUser user = _context.Users.Where(r => r.Email == userobj.email).First();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, userobj.newpass);
                if (result.Succeeded)
                {

                    return Content(JsonConvert.SerializeObject(new returnMsg { message = "Your Password has been successfully changed." }));

                }
                else
                {
                    var exceptionText = result.Errors.First();
                    throw new Exception(exceptionText.Description);
                }

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }
        }


        [HttpGet("roles")]
        public async Task<IActionResult> allroles()
        {
            try
            {
                var roles = _roleManager.Roles.Select(r => new
                {
                    Name = r.Name,
                    Id = r.Id
                }).ToList();
                return Ok(JsonConvert.SerializeObject(roles));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpGet]
        [Route("userroles")]
        public async Task<IActionResult> userrole([FromQuery]string email)
        {
            try
            {
                IdentityUser user = _context.Users.Where(r => r.Email.Equals(email)).First();
                var userroles = await _userManager.GetRolesAsync(user);
                return Ok(JsonConvert.SerializeObject(userroles));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpPost]
        [Route("addrole")]
        public async Task<IActionResult> AddRole([FromBody]addrole userobj)
        {
            try
            {

                IdentityUser user = _context.Users.Where(r => r.Email == userobj.email).First();
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, userobj.role))
                    {
                        throw new Exception("Role Already Exist");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, userobj.role);
                        return Content(JsonConvert.SerializeObject(new returnMsg { message = userobj.role + " Role Added" }));
                    }
                }
                else
                {
                    throw new Exception("User Not Found");
                }

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }
        }

        [HttpPost]
        [Route("removerole")]
        public async Task<IActionResult> RemoveRole([FromBody]addrole userobj)
        {
            try
            {

                IdentityUser user = _context.Users.Where(r => r.Email == userobj.email).First();
                if (user != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, userobj.role);
                    return Content(JsonConvert.SerializeObject(new returnMsg { message = userobj.role + " Role Removed" }));
                }
                else
                {
                    throw new Exception("User Not Found");
                }

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }
        }

        [HttpGet]
        [Route("getUserFullname")]
        public async Task<IActionResult> getUserFullname([FromQuery]string id)
        {
            try
            {
                var fullName = _context.UserProfile.Where(q => q.Id.ToString().Equals(id))
                                                   .Select(s => s.FirstName + ' ' + s.LastName)
                                                   .FirstOrDefault();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(fullName));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }

        [HttpGet]
        [Route("getDietitianName")]
        public async Task<IActionResult> getDietitianName([FromQuery]string patientId)
        {
            try
            {
                if (string.IsNullOrEmpty(patientId))
                {
                    var name = _context.UserProfile.Join(
                        _context.Group,
                        u => u.Id,
                        g => g.DieticianId,
                        (u, g) => new
                        {
                            u.FirstName,
                            u.LastName
                        }).Distinct();

                    return Ok(JsonConvert.SerializeObject(name));
                }
                else
                {
                    /*
                    var dietitianId = _context.Group.Where(g => g.Id.ToString().Equals(groupId))
                                                    .Select(s => s.DieticianId)
                                                    .FirstOrDefault();

                    var name = _context.UserProfile.Where(q => q.Id.ToString().Equals(dietitianId.ToString()))
                                                   .Select(d => new { d.FirstName, d.LastName })
                                                   .FirstOrDefault();*/

                    var groupId = _context.UserProfile.Where(s => s.Id.ToString().Equals(patientId))
                                                      .Select(g => g.GroupId)
                                                      .FirstOrDefault();

                    var dietitianId = _context.Group.Where(g => g.Id.ToString().Equals(groupId.ToString()))
                                                    .Select(s => s.DieticianId)
                                                    .FirstOrDefault();

                    var name = _context.UserProfile.Where(q => q.Id.ToString().Equals(dietitianId.ToString()))
                                                   .Select(d => new { d.FirstName, d.LastName })
                                                   .FirstOrDefault();

                    return Ok(JsonConvert.SerializeObject(name));
                }
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
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

        /*
        [HttpGet]
        [Route("getDieticianFullname")]
        public async Task<IActionResult> getUserFullname([FromQuery]string id)
        {
            try
            {
                var fullName = _context.UserProfile.Where(q => q.Id.ToString().Equals(id))
                                                   .Select(s => s.FirstName + ' ' + s.LastName)
                                                   .FirstOrDefault();
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(fullName));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, JsonConvert.SerializeObject(new returnMsg { message = e.Message }));
            }

        }*/

        //models
        public class changepassword
        {
            public string email;
            public string newpass;
        }

        public class addrole
        {
            public string email;
            public string role;
        }

        public class NewUser
        {
            public string Email;
            public string Password;
            public string FirstName;
            public string LastName;
            public string PhoneNumber;
            public string[] rolestring;
        }
    }
}
