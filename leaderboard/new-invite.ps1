$ErrorActionPreference = 'Stop'
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $bytes = New-Object byte[] 32; $random.GetBytes($bytes) }
finally { $random.Dispose() }
$code = [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [Security.Cryptography.SHA256]::Create()
try { $hash = [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($code))).Replace('-', '').ToLowerInvariant() }
finally { $hasher.Dispose() }
$playerId = [Guid]::NewGuid().ToString('N')
Write-Output "Give this invite code privately to one friend: $code"
Write-Output "Add its hash to D1 with this SQL (the code itself is never stored there):"
Write-Output "INSERT INTO invites (code_hash, player_id) VALUES ('$hash', '$playerId');"
