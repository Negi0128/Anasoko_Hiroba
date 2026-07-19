$ErrorActionPreference = 'Stop'

# 共有コア（Shared/AnasPack）のヘッドレス検証。
# 仕様書の検証4項目 (a)dan_hash一致 (b)導入→再計算一致 (c)ごほうびround-trip (d)Zip Slip拒否 を自動確認する。

$repo   = "C:\Users\admin\Desktop\プログラミング\Anasoko_Hiroba"
$shared = Join-Path $repo "Shared\AnasPack"
$core   = Join-Path $repo "AnasCore.dll"
$daniSrc = "C:\Users\admin\Documents\GitHub\Dani\001-Monitor用\14,十段"
$expectedHash = "944b4997-f995f658-93e615cd-9103ec22"

# AnasCore.dll はネットDL由来でブロックされていることがあり LoadFrom は 0x80131515 で失敗する。
# バイト列から読み込み、コンパイル済み共有コードの実行時参照解決にも同じインスタンスを返す。
$coreAsm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($core))
[System.AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($s, $e)
    if ($e.Name -like 'AnasCore*') { return $coreAsm }
    return $null
})

$srcs = @('PackManifest.cs','PackCrypto.cs','DanBinReader.cs','DanDefinition.cs','AnasPackFile.cs') |
    ForEach-Object { Join-Path $shared $_ }
$refs = @($core, 'System.Web.Extensions.dll', 'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll')
Add-Type -Path $srcs -ReferencedAssemblies $refs

$work = Join-Path $env:TEMP ("ansktest_" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work -Force | Out-Null

function Assert($cond, $msg) {
    if ($cond) { Write-Host "  OK  $msg" } else { throw "  NG  $msg" }
}
function Sha256($path) {
    (Get-FileHash -Algorithm SHA256 -Path $path).Hash
}
# 指定エントリ名/内容だけを持つ zip のバイト列を作る（Zip Slip 検証用）
function MakeZipBytes([string]$entryName, [string]$content) {
    $ms = New-Object System.IO.MemoryStream
    $za = New-Object System.IO.Compression.ZipArchive($ms, [System.IO.Compression.ZipArchiveMode]::Create, $true)
    $entry = $za.CreateEntry($entryName)
    $sw = New-Object System.IO.StreamWriter($entry.Open())
    $sw.Write($content); $sw.Dispose()
    $za.Dispose()
    $bytes = $ms.ToArray(); $ms.Dispose()
    return ,$bytes
}

try {
    # ---- (a) 十段の dan_hash 一致 ----
    Write-Host "(a) DanDefinition dan_hash"
    $dan = New-Object AnasPack.DanDefinition($daniSrc)
    Write-Host "    DanTitle=$($dan.DanTitle)  DanHash=$($dan.DanHash)  TJA=$($dan.ReferencedTjaFiles.Count)"
    Assert ($dan.DanHash -eq $expectedHash) "dan_hash が期待値と一致"

    # ---- パック作成の下ごしらえ ----
    $rewardSrc = Join-Path $work "rewardsrc"
    New-Item -ItemType Directory -Path $rewardSrc -Force | Out-Null
    Set-Content -Path (Join-Path $rewardSrc "song.tja") -Value "TITLE:UnlockSong`nreward body" -Encoding UTF8
    [System.IO.File]::WriteAllBytes((Join-Path $rewardSrc "song.ogg"), (1..500 | ForEach-Object { [byte]($_ % 256) }))

    $manifest = New-Object AnasPack.PackManifest
    $manifest.format_version = 2
    $manifest.name = "十段チャレンジ解禁パック"
    $manifest.author = "Negi"
    $manifest.dan_title = $dan.DanTitle
    $manifest.dan_hash = $dan.DanHash
    $manifest.condition = "pass"
    $manifest.message = "十段合格おめでとう！"
    $manifest.dan_install_folder = "十段チャレンジ"
    $rp = New-Object 'System.Collections.Generic.List[string]'
    $rp.Add("解禁曲/新曲A")
    $manifest.reward_paths = $rp

    $danZip = [AnasPack.AnasPackFile]::BuildDanZip($daniSrc)
    $kvList = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
    $kv = New-Object 'System.Collections.Generic.KeyValuePair[string,string]' -ArgumentList $rewardSrc, "解禁曲/新曲A"
    $kvList.Add($kv)
    $rewardZip = [AnasPack.AnasPackFile]::BuildRewardZip($kvList)

    $packPath = Join-Path $work "test.anskpack"
    [AnasPack.AnasPackFile]::Create($packPath, $manifest, $danZip, $rewardZip)
    Assert (Test-Path $packPath) "パック生成"

    # ---- (b) 作成→ReadManifest→InstallDan→再計算一致 ----
    Write-Host "(b) InstallDan -> DanDefinition 再計算"
    $m2 = [AnasPack.AnasPackFile]::ReadManifest($packPath)
    Assert ($m2.dan_hash -eq $dan.DanHash -and $m2.dan_install_folder -eq "十段チャレンジ") "ReadManifest 内容一致"
    Assert ($null -eq $m2.Validate()) "manifest.Validate() が null（正常）"

    $daniRoot = Join-Path $work "Dani"
    $installDir = [AnasPack.AnasPackFile]::InstallDan($packPath, $daniRoot)
    Write-Host "    installDir=$installDir"
    Assert (Test-Path (Join-Path $installDir "dani.json")) "導入先に dani.json が復元"
    Assert (Test-Path (Join-Path $installDir "fumen")) "導入先に fumen が復元"
    $dan2 = New-Object AnasPack.DanDefinition($installDir)
    Assert ($dan2.DanHash -eq $expectedHash) "導入後 dani.json からの再計算が一致"

    # ---- (c) reward round-trip（SHA256一致）----
    Write-Host "(c) reward round-trip"
    $songsOut = Join-Path $work "Songs"
    $extracted = [AnasPack.AnasPackFile]::ExtractReward($packPath, $songsOut)
    Write-Host "    extracted: $($extracted -join ', ')"
    $tjaOrig = Join-Path $rewardSrc "song.tja"
    $oggOrig = Join-Path $rewardSrc "song.ogg"
    $tjaOut  = Join-Path $songsOut "解禁曲/新曲A/song.tja"
    $oggOut  = Join-Path $songsOut "解禁曲/新曲A/song.ogg"
    Assert (Test-Path $tjaOut) "song.tja 展開先に存在"
    Assert (Test-Path $oggOut) "song.ogg 展開先に存在"
    Assert ((Sha256 $tjaOrig) -eq (Sha256 $tjaOut)) "song.tja SHA256一致"
    Assert ((Sha256 $oggOrig) -eq (Sha256 $oggOut)) "song.ogg SHA256一致"

    # ---- (d) Zip Slip 拒否（InstallDan / ExtractReward）----
    Write-Host "(d) Zip Slip 拒否"
    $slipDanFile = Join-Path $work "SLIP_DAN.txt"    # Dani ルートの外（work直下）へ出ようとする
    $slipRwdFile = Join-Path $work "SLIP_REWARD.txt"

    # 悪意 dan.zip（"../../SLIP_DAN.txt"）を持つパックを作る
    $evilDanZip = MakeZipBytes "../../SLIP_DAN.txt" "pwned"
    $evilRewardZip = MakeZipBytes "../../SLIP_REWARD.txt" "pwned"  # 平文の内部zip（Create が暗号化して格納）
    $evilPack = Join-Path $work "evil.anskpack"
    [AnasPack.AnasPackFile]::Create($evilPack, $manifest, $evilDanZip, $evilRewardZip)

    $slipDaniRoot = Join-Path $work "SlipDani"
    $threw = $false
    try { [AnasPack.AnasPackFile]::InstallDan($evilPack, $slipDaniRoot) } catch { $threw = $true; Write-Host "    Install: $($_.Exception.Message)" }
    Assert $threw "InstallDan が Zip Slip を例外で拒否"
    Assert (-not (Test-Path $slipDanFile)) "InstallDan でルート外にファイルが出ていない"

    $slipSongs = Join-Path $work "SlipSongs"
    $threw2 = $false
    try { [AnasPack.AnasPackFile]::ExtractReward($evilPack, $slipSongs) } catch { $threw2 = $true; Write-Host "    Extract : $($_.Exception.Message)" }
    Assert $threw2 "ExtractReward が Zip Slip を例外で拒否"
    Assert (-not (Test-Path $slipRwdFile)) "ExtractReward でルート外にファイルが出ていない"

    Write-Host ""
    Write-Host "ALL TESTS PASSED"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
