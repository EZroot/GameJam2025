:: Windows batch script: convert all audio to OGG (Vorbis)
for %%a in (*.mp3 *.wav *.flac *.aac *.m4a *.wma) do (
    ffmpeg -y -i "%%a" -c:a libvorbis -qscale:a 5 "%%~na.ogg"
)
