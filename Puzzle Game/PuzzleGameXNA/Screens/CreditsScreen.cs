using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleGameXNA.Screens
{
    class CreditsScreen : MenuScreen
    {
        MenuEntry title, authorName, publisher, url;

        public CreditsScreen()
            : base("Credits")
        {
            title = new MenuEntry("Title: Chris' Puzzle Game");
            authorName = new MenuEntry("Author: Christian Resma Helle");
            publisher = new MenuEntry("Publisher: Commentor AppFabric");
            url = new MenuEntry("http://www.commentor.dk");

            MenuEntries.Add(title);
            MenuEntries.Add(authorName);
            MenuEntries.Add(publisher);
            MenuEntries.Add(url);
        }
    }
}
