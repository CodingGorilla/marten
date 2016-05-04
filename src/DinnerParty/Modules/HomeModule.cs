using System;

namespace DinnerParty.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule()
        {
            Get["/"] = parameters =>
            {
                base.Page.Title = "Home";

                return View["Index", base.Model];
            };

            Get["/about"] = parameters =>
            {
               
                base.Page.Title = "About";

                return View["About", base.Model];
            };
        }
    }
}