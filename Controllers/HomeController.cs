using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BridgeMonitor.Models;
using Newtonsoft.Json;
using System.Net.Http;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Text;

namespace BridgeMonitor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<BoatModel> boats = GetBoats();
            boats.Sort((s1, s2) => DateTimeOffset.Compare(s1.ClosingDate, s2.ClosingDate));

            foreach (var boatModel in boats)
            {
                if (DateTimeOffset.Compare(DateTimeOffset.Now, boatModel.ClosingDate) < 0)
                {
                    ViewData["Boat"] = boatModel;
                    break;
                }
            }

            return View();
        }

        public IActionResult Detail(string boat)
        {
            string[] infos = boat.Split(".");
            List<BoatModel> boats = GetBoats();
            List<BoatModel> oldBoats = new List<BoatModel>();
            for (int i = 0; i < boats.Count; i++)
            {
                if (boats[i].ClosingDate.CompareTo(DateTime.Now) < 0)
                {
                    oldBoats.Add(boats[i]);
                    boats.RemoveAt(i);
                }
            }
            // Sort
            boats.Sort((s1, s2) => DateTimeOffset.Compare(s1.ClosingDate, s2.ClosingDate));
            oldBoats.Sort((s1, s2) => DateTimeOffset.Compare(s1.ClosingDate, s2.ClosingDate));

            ViewData["Boat"] = infos[0] == "n" ? boats[int.Parse(infos[1])] : oldBoats[int.Parse(infos[1])];
            ViewData["Detail"] = true;
            return View("Index");
        }

        public IActionResult All()
        {
            ViewData["Boats"] = GetBoats();
            return View();
        }

        public IActionResult DownloadCalendar()
        {
            List<BoatModel> boats = GetBoats();
            for (int i = 0; i < boats.Count; i++)
            {
                if (boats[i].ClosingDate.CompareTo(DateTime.Now) < 0)
                {
                    boats.RemoveAt(i);
                }
            }
            // Sort
            boats.Sort((s1, s2) => DateTimeOffset.Compare(s1.ClosingDate, s2.ClosingDate));

            // Create calendar
            var calendar = new Calendar();
            foreach (BoatModel boat in boats)
            {
                var icalEvent = new CalendarEvent
                {
                    Name = boat.BoatName,
                    Description = string.Format("Type {0}", boat.ClosingType),
                    Start = new CalDateTime(boat.ClosingDate),
                    End = new CalDateTime(boat.ReopeningDate)
                };

                calendar.Events.Add(icalEvent);
            }

            var iCalSerializer = new CalendarSerializer();
            string result = iCalSerializer.SerializeToString(calendar);

            return File(Encoding.ASCII.GetBytes(result), "calendar/text", "calendar.ics");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public static List<BoatModel> GetBoats()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("https://api.alexandredubois.com/pont-chaban/api.php");
                var stringResult = response.Result.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<BoatModel>>(stringResult.Result);
                return result;
            }
        }
    }
}
