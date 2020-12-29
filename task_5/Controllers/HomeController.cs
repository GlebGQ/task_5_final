using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using task_5.Models;
using task_5.Services;

namespace task_5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private GameService gameService;
        private Models.AppContext appContext;
        public HomeController(ILogger<HomeController> logger,GameService gameService, Models.AppContext appContext)
        {
            _logger = logger;
            this.gameService = gameService;
            this.appContext = appContext;
        }

        [Authorize]
        public IActionResult Game()
        {
            var player = gameService.GetPlayer(User.Identity.Name);
            var game = gameService.GetGame(Request.RouteValues["gameId"].ToString());

            if(player == null || game == null)
            {
                return RedirectToAction("Lobby");
            }

            if(game.Player1?.Name != player.Name && game.Player2?.Name != player.Name)
            {
                return RedirectToAction("Lobby");
            }
            else
            {
                return View();
            }
        }

        [Authorize]
        public IActionResult Lobby()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetTags()
        {
            List<string> tags = appContext.Tags.Select(t => t.Name).ToList();
            return Json(tags);
        }

        [HttpPost]
        public IActionResult SaveTags([FromBody] List<string> tags)
        {
            appContext.AddRange(tags.Select(t => new Tag { Name = t.Trim() }));
            try
            {
                appContext.SaveChanges();
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
