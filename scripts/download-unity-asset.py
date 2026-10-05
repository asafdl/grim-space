#!/usr/bin/env python3
"""Download one purchased Unity Asset Store package and unpack its raw files.

The Asset Store session cookie is read from UNITY_ASSET_STORE_COOKIE or from a
hidden prompt. It is never written to disk.

Usage:
  python3 scripts/download-unity-asset.py "Post Apocalypse Robots"
  python3 scripts/download-unity-asset.py 12345 --output ~/Downloads/unity-assets
  python3 scripts/download-unity-asset.py "Post Apocalypse Robots" --package-only
"""

from __future__ import annotations

import argparse
import getpass
import json
import os
import re
import shutil
import sys
import tarfile
from pathlib import Path, PurePosixPath
from urllib.error import HTTPError, URLError
from urllib.parse import unquote, urljoin, urlparse
from urllib.request import HTTPRedirectHandler, Request, build_opener, urlopen

GRAPHQL_URL = "https://assetstore.unity.com/api/graphql/batch"
DOWNLOAD_URL = "https://assetstore.unity.com/api/downloads"
USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/146 Safari/537.36"
SEARCH_QUERY = """
query SearchMyAssets(
  $page: Int, $pageSize: Int, $q: [String], $tagging: [String!],
  $assignFrom: [String!], $ids: [String!], $sortBy: Int
) {
  searchMyAssets(
    page: $page, pageSize: $pageSize, q: $q, tagging: $tagging,
    assignFrom: $assignFrom, ids: $ids, sortBy: $sortBy
  ) {
    results {
      product {
        id
        name
        downloadSize
        publisher { name }
      }
    }
    total
  }
}
"""


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def fail(message: str) -> None:
    sys.exit(f"error: {message}")


def get_cookie() -> str:
    cookie = os.environ.get("UNITY_ASSET_STORE_COOKIE")
    if not cookie:
        cookie = getpass.getpass("Paste the Asset Store Cookie request header: ")
    if not cookie.strip():
        fail("the Asset Store cookie is empty")
    if not re.search(r"(?:^|;\s*)_csrf=([^;]+)", cookie):
        fail("the cookie does not contain the required _csrf value")
    return cookie.strip()


def common_headers(cookie: str) -> dict[str, str]:
    csrf = re.search(r"(?:^|;\s*)_csrf=([^;]+)", cookie)
    assert csrf
    return {
        "Accept": "application/json, text/plain, */*",
        "Content-Type": "application/json;charset=UTF-8",
        "Cookie": cookie,
        "Operations": "SearchMyAssets",
        "Origin": "https://assetstore.unity.com",
        "Referer": "https://assetstore.unity.com/",
        "User-Agent": USER_AGENT,
        "X-CSRF-Token": unquote(csrf.group(1)),
        "X-Requested-With": "XMLHttpRequest",
        "X-Source": "storefront",
    }


def fetch_page(cookie: str, page: int, page_size: int = 100) -> dict:
    payload = [{
        "operationName": "SearchMyAssets",
        "query": SEARCH_QUERY,
        "variables": {
            "page": page,
            "pageSize": page_size,
            "q": [],
            "tagging": [],
            "assignFrom": [],
            "ids": [],
            "sortBy": 7,
        },
    }]
    request = Request(
        GRAPHQL_URL,
        data=json.dumps(payload).encode(),
        headers=common_headers(cookie),
        method="POST",
    )
    try:
        with build_opener(NoRedirect).open(request, timeout=60) as response:
            body = json.load(response)
    except HTTPError as error:
        if error.code in (401, 403):
            fail("Unity rejected the session cookie; sign in again and copy a fresh Cookie header")
        fail(f"Unity asset query returned HTTP {error.code}")
    except (URLError, TimeoutError) as error:
        fail(f"Unity asset query failed: {error}")
    except json.JSONDecodeError:
        fail("Unity returned a non-JSON response for the asset query")

    if not isinstance(body, list) or not body:
        fail("Unity returned an unexpected GraphQL response")
    if body[0].get("errors"):
        messages = "; ".join(str(item.get("message", item)) for item in body[0]["errors"])
        fail(f"Unity GraphQL error: {messages}")
    try:
        return body[0]["data"]["searchMyAssets"]
    except (KeyError, TypeError):
        fail("Unity's asset-list response format has changed")


def fetch_assets(cookie: str) -> list[dict]:
    assets: list[dict] = []
    page = 0
    while True:
        result = fetch_page(cookie, page)
        products = [item["product"] for item in result.get("results", []) if item.get("product")]
        assets.extend(products)
        total = int(result.get("total") or 0)
        if not products or len(assets) >= total:
            return assets
        page += 1


def select_asset(assets: list[dict], requested: str) -> dict:
    if requested.isdigit():
        matches = [asset for asset in assets if str(asset.get("id")) == requested]
    else:
        wanted = requested.casefold()
        matches = [asset for asset in assets if str(asset.get("name", "")).casefold() == wanted]
        if not matches:
            matches = [asset for asset in assets if wanted in str(asset.get("name", "")).casefold()]
    if len(matches) == 1:
        return matches[0]
    if not matches:
        fail(f"no purchased asset matched {requested!r}")
    choices = "\n".join(f"  {asset['id']}: {asset['name']}" for asset in matches[:20])
    fail(f"multiple assets matched; rerun with a numeric asset ID:\n{choices}")


def response_filename(response, fallback: str) -> str:
    disposition = response.headers.get("Content-Disposition", "")
    match = re.search(r'filename\*?=(?:UTF-8\'\')?"?([^";]+)', disposition, re.IGNORECASE)
    filename = Path(unquote(match.group(1) if match else fallback)).name
    return filename if filename.endswith(".unitypackage") else f"{filename}.unitypackage"


def open_download(asset_id: str, cookie: str):
    request = Request(
        f"{DOWNLOAD_URL}/{asset_id}",
        headers={
            "Accept": "*/*",
            "Cookie": cookie,
            "Referer": "https://assetstore.unity.com/",
            "User-Agent": USER_AGENT,
        },
    )
    opener = build_opener(NoRedirect)
    try:
        return opener.open(request, timeout=300)
    except HTTPError as error:
        if error.code in (301, 302, 303, 307, 308):
            location = urljoin(error.url, error.headers.get("Location", ""))
            if urlparse(location).scheme != "https":
                fail("Unity returned an unsafe download redirect")
            return urlopen(
                Request(location, headers={"Accept": "*/*", "User-Agent": USER_AGENT}),
                timeout=300,
            )
        if error.code in (401, 403):
            fail("Unity rejected this download; verify the cookie and that the asset is owned")
        fail(f"Unity download returned HTTP {error.code}")
    except (URLError, TimeoutError) as error:
        fail(f"Unity download failed: {error}")


def download(asset: dict, cookie: str, output: Path, force: bool) -> Path:
    fallback = re.sub(r"[^A-Za-z0-9._ -]+", "_", str(asset["name"])).strip() or str(asset["id"])
    with open_download(str(asset["id"]), cookie) as response:
        destination = output / response_filename(response, fallback)
        partial = destination.with_suffix(destination.suffix + ".part")
        if destination.exists() and not force:
            fail(f"package already exists: {destination} (pass --force to replace it)")
        total = int(response.headers.get("Content-Length") or asset.get("downloadSize") or 0)
        downloaded = 0
        output.mkdir(parents=True, exist_ok=True)
        try:
            with partial.open("wb") as stream:
                while chunk := response.read(1024 * 1024):
                    stream.write(chunk)
                    downloaded += len(chunk)
                    status = f"{downloaded / 1024 / 1024:.1f} MiB"
                    if total:
                        status += f" / {total / 1024 / 1024:.1f} MiB ({downloaded / total:.0%})"
                    print(f"\rDownloading {asset['name']}: {status}", end="", flush=True)
            os.replace(partial, destination)
        except BaseException:
            partial.unlink(missing_ok=True)
            raise
    print(f"\nSaved {destination}")
    return destination


def extract_package(package: Path, force: bool) -> Path:
    root = package.with_suffix("")
    with tarfile.open(package, "r:*") as archive:
        groups: dict[str, dict[str, tarfile.TarInfo]] = {}
        for member in archive.getmembers():
            parts = PurePosixPath(member.name).parts
            if member.isfile() and len(parts) == 2:
                groups.setdefault(parts[0], {})[parts[1]] = member
        planned: list[tuple[tarfile.TarInfo, Path]] = []
        for files in groups.values():
            if "pathname" not in files or "asset" not in files:
                continue
            pathname_stream = archive.extractfile(files["pathname"])
            if pathname_stream is None:
                continue
            pathname = pathname_stream.read(32768).decode("utf-8").strip("\x00\r\n")
            relative = PurePosixPath(pathname)
            if relative.is_absolute() or ".." in relative.parts or not relative.parts:
                fail(f"unsafe path inside package: {pathname!r}")
            destination = root.joinpath(*relative.parts)
            if destination.exists() and not force:
                fail(f"extracted file already exists: {destination} (pass --force to replace it)")
            planned.append((files["asset"], destination))
        if not planned:
            fail("the downloaded file is not a recognizable Unity asset package")
        for member, destination in planned:
            source = archive.extractfile(member)
            if source is None:
                continue
            destination.parent.mkdir(parents=True, exist_ok=True)
            with source, destination.open("wb") as target:
                shutil.copyfileobj(source, target)
    print(f"Extracted {len(planned)} files to {root}")
    return root


def main() -> None:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("asset", help="Exact/partial purchased asset name, or numeric product ID")
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("~/Downloads/unity-assets"),
        help="Download directory",
    )
    parser.add_argument("--package-only", action="store_true", help="Do not unpack raw files")
    parser.add_argument("--force", action="store_true", help="Replace existing package/extracted files")
    args = parser.parse_args()

    cookie = get_cookie()
    print("Reading purchased assets...")
    asset = select_asset(fetch_assets(cookie), args.asset)
    publisher = (asset.get("publisher") or {}).get("name", "unknown publisher")
    print(f"Selected {asset['name']} ({asset['id']}) by {publisher}")
    package = download(asset, cookie, args.output.expanduser().resolve(), args.force)
    if not args.package_only:
        extract_package(package, args.force)


if __name__ == "__main__":
    main()
