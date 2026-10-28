param (
    [string]$DocxPath,
    [string]$PdfPath
)

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $doc = $word.Documents.Open($DocxPath)
    $wdFormatPDF = 17
    $doc.SaveAs([ref]$PdfPath, [ref]$wdFormatPDF)
    $doc.Close()
    $word.Quit()
    Write-Output "SUCCESS"
} catch {
    Write-Output "ERROR: $_"
}
