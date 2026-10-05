# Unity Asset Store downloader

`download-unity-asset.py` downloads one asset owned by the signed-in Unity
account and extracts the raw files from its `.unitypackage`. It uses
undocumented Unity Asset Store endpoints and may need maintenance if Unity
changes them.

## Download an asset

1. Sign in at `assetstore.unity.com` and open **My Assets**.
2. Open browser DevTools, select **Network**, and reload the page.
3. Select the `/api/graphql/batch` request and copy the complete **Cookie**
   value under **Request Headers**.
4. Run the downloader while the cookie remains on the macOS clipboard:

   ```sh
   UNITY_ASSET_STORE_COOKIE="$(pbpaste)" \
     python3 scripts/download-unity-asset.py "Post Apocalypse Robots"
   ```

The package and extracted `Assets/` tree are written to
`~/Downloads/unity-assets/` by default. Use a numeric product ID instead of the
name if several owned assets match.

Options:

- `--output PATH` changes the download directory.
- `--package-only` keeps the `.unitypackage` without extracting it.
- `--force` replaces an existing package and extracted files.

The cookie is sent only to `assetstore.unity.com` and is never written to disk.
Unity may redirect the package download to an HTTPS CDN; the downloader follows
that redirect without forwarding the cookie. Clear the clipboard afterward:

```sh
printf '' | pbcopy
```
