using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace RivetBlazor.Classes
{
    // Preserved from an old release of RivetBlazor
    public class HeaderProfile : HeaderDropdown
    {
        public HeaderProfile(string username, IEnumerable<HeaderLink> links, string title = "", bool isLoggedIn = false, RenderFragment decoration = null) : base(username, links, title, "", decoration)
        {
            IsLoggedIn = isLoggedIn;
        }

        public bool IsLoggedIn { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }

        public event Action OnProfileChanged;

        public HeaderLink ConvertToLink()
        {
            var url = Links != null && Links.Any() ? Links.First().Uri : string.Empty;
            return new HeaderLink(url, Username, Title, Decorations.ToArray());
        }

        public HeaderDropdown ConvertToDropdown() =>
            new(Username, Links, Title, string.Empty, Decorations.ToArray());

        public void ProfileChanged() => OnProfileChanged.Invoke();
    }
}