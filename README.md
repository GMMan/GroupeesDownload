Groupees Scraper
================

This program is designed to scrape a copy of your Groupees account, including
bundles, products, keys, and various download links. To help with archival, it
can automatically remove items from trade or giveaway, and automatically
reveal all products and keys. A download list can be generated that includes
both product files and covers.

Usage
-----
In the interest of getting things out quickly, the program is currently being
released as-is with no proper CLI, so you will need something that is capable
of compiling .NET programs and modify `Program.cs` to have things run. See
`Program.cs` for some of the things you can do.

You will need your account ID, your session cookie, and the CSRF token to
run this.

TODOs
-----
- Add CLI
- Add parsing for new profile tracks and comics
- Handle giveaway detection
- Try to remove from trade with trades site instead of main site (main site
  seems a bit buggy)

Help wanted
-----------
I don't have any unrevealed products/keys in my account anymore, so I need
someone's help to get the HTML from the new profile so I can parse how those
things work, for the purposes of scraping products obtained from trading.
