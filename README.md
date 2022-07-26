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

To interact with your account, you need your session cookie. First, visit [groupees.com](https://groupees.com)
and log in. To get the session cookie, check the instructions [here](https://www.cookieyes.com/blog/how-to-check-cookies-on-your-website-manually/),
looking specifically for `_groupees_session`. Copy the corresponding value,
and in a text document type `--cookie`, followed by a space, then a `"`,
and paste the cookie value. Finish by typing another `"`. Keep this file
around so you can copy from it as you run the program. The cookie is valid
indefinitely.

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

Note: it is currently unknown how the program behaves when it encounters a
traded bundle, since I don't have any in my account. If you encounter issues,
please file an issue.

### Dumping your own third party keys

You can also export third party keys you have added.

```
GroupeesDownload.exe dump-third-party-keys <tokens>
```

Please make sure to specify your bundles DB path and trades DB path if you are
not using the defaults, otherwise due to how Groupees exposes information,
all products in your account will be enumerated instead of just the ones that
have not already been dumped.

The dump of your third party key products will be saved to `third_party_keys.json`
by default.

Note that third party keys work like any other key, and will not be visible
until revealed.

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

The actions are only applied to bundles and trades, or third party keys. To
apply actions to third party keys, specify `--tpk-db` if you are not using
the default third party key DB path, and then specify `--for-tpk`.

If you want to apply the action to only certain items, replace `--all` with
a space-separated list of trade/giveaway/product IDs (find them from your DB
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
You can remove them with the `--dedupe` option.

Cover images are also included. If you don't care for them, specify the
`--no-covers` option.

If you want to only generate links for certain types of products, you can mix
and match the following options:

- `--filter-games`: include games
- `--filter-music`: include music
- `--filter-others`: include anything that's not games or music

If you do not specify one of the above options, all links will be added to
list.

By default for music, if FLAC files are available, they will be chosen instead
of MP3 files. If you want both, add the `--include-all` option.

You can use your favorite downloads manager to import these links. Note that
the download manager must support cookies or custom headers so you can
authenticate with the storage server. I use [aria2c](https://aria2.github.io/).

If you are using aria2, you can also specify how to place files into
directories. The `--organize` option takes the following values:

- `BundleOnly`: only sorts files by bundle
- `BundleAndProduct`: sorts file by bundle, then by product name
- `BundleAndType`: sorts files by bundle, then by product type (games,
  music, android, books, movies, others)

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
