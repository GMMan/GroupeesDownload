Groupees Scraper
================

This program is designed to scrape a copy of your Groupees account, including
bundles, products, keys, and various download links. To help with archival, it
can automatically remove items from trade or giveaway, and automatically
reveal all products and keys. A download list can be generated that includes
both product files and covers (with extended support for Aria2), and you can
also export a spreadsheet of all your keys.

If you've found this useful, please consider tipping me on [Ko-Fi](https://ko-fi.com/caralynx).

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/C0C81P4PX)

Tutorial
--------

### Prerequisites

This is a .NET Core 3.1 app, and as such, you need to have the runtime
installed. Download from [here](https://dotnet.microsoft.com/en-us/download/dotnet/3.1)
if you do not.

### Getting your tokens

To interact with your account, you need three pieces of information: user ID,
session cookie, and CSRF token. Visit [groupees.com](https://groupees.com),
log in, open up the developer console in your browser
(Ctrl-Shift-C on Chrome, Ctrl-Shift-J on Firefox), and enter the following:

```js
const userId = document.body.querySelector('.fayepub').getAttribute('data-user');
const csrfToken = document.getElementsByName('csrf-token')[0].getAttribute('content');
console.log('--user-id ' + userId + ' --csrf-token "' + csrfToken + '"');
```

This will print the user ID and CSRF token that you can pass to the program.
Copy and paste this to a blank text document for later.

To get the session cookie, check the instructions [here](https://www.cookieyes.com/blog/how-to-check-cookies-on-your-website-manually/),
looking specifically for `_groupees_session`. Copy the corresponding value,
and at the end of the text document you've previously created, type
`--cookie`, followed by a space, then a `"`, and paste the cookie value.
Finish by typing another `"`. Keep this file around so you can copy from it
as you run the program. The tokens are valid indefinitely.

When you see `<tokens>` in the instructions below, replace it with the contents
of this text document.

### Dumping bundles

To dump bundles, run the program like this:

```
GroupeesDownload.exe dump-bundles <tokens>
```

This will save the metadata of all your bundles and their products to a file
named `bundles.json` in the current working directory. You can specify a
different file name to save to by specifying it with the `--bundles-db` option.

### Dumping trade products

To dump products you've obtained from trading, run the program like this:

```
GroupeesDownload.exe dump-trades <tokens>
```

This will save the metadata of all the products you've received from trades to
a file named `trades.json` in the current working directory. Similar to,
bundles, you can change this name by specifying the `--trades-db` option.

**You may have to reobtain tokens for the trades site specifically.**

Note: it is currently unknown how the program behaves when it encounters a
traded bundle, since I don't have any in my account. If you encounter issues,
please file an issue.

### Unmarking trades, giveaways, and revealing products and keys

You can unmark from trades and giveaways, and reveal products and keys
automatically. Run the program like this:

```
GroupeesDownload.exe <command> <tokens> --all
```

Where `<command>` is one of:
- `unmark-trades`
- `unmark-giveaway`
- `reveal-products`
- `reveal-keys`

This will apply the action to all eligible items. Your database will be
automatically updated with refreshed bundle/product data.

If your databases are on a different path, specify `--bundles-db` and
`--trades-db` as needed.

If you want to apply the action to only certain items, replace `--all` with
a space separated list of trade/giveaway/product IDs (find them from your DB
files). Your databases will not be automatically updated, so make sure to
redump them using earlier commands.

### Generating links

You can generate all the download links to back up all the files in your
account. Run the program like this:

```
GroupeesDownload.exe generate-links
```

If your databases are on a different path, specify `--bundles-db` and
`--trades-db` as needed.

This will export all links to `downloads_list.txt`. Use the `--output` option
if you would like the file saved somewhere else.

There may be duplicate links if you have obtained a product multiple times.
Cover images are also included. If you don't care for them, specify the
`--no-covers` option.

You can use your favorite downloads manager to import these links. Note that
the download manager must support cookies or custom headers so you can
authenticate with the storage server. I use [aria2c](https://aria2.github.io/).

If you are using aria2, you can also specify the `--use-dirs` option to have
all files automatically placed into directories named after their bundle name.

aria2c example:

```
aria2c -i downloads_list.txt --header "Cookie: _groupees_session=<cookie>"
```

Replace `<cookie>` with your proper session cookie value.

### Exporting keys

You can also export all the keys from your account. Run the program like
this:

```
GroupeesDownload.exe export-keys
```

This will export all keys to `keys.csv`. Use the `--output` option
if you would like the file saved somewhere else.
