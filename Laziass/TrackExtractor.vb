﻿Imports System.IO
Imports BizHawk.Emulation.DiscSystem

Module TrackExtractor
    Public RemSector As Integer = 0

    Public Sub Extract(ByVal disc2 As Disc, ByVal Dpath As String, ByVal filebase As String)
        Dim dsr = New DiscSectorReader(disc2)
        Dim confirmed As Boolean = False
        Dim tracks = disc2.Session1.Tracks
        Dim TrackData() As Byte
        Dim RemSector As Integer = 0

        If LazyAss.ResultDisc = "SonyPSX" Then
            Dim msgSect = MsgBox("Do you want to remove 150 blocks at EOF?", vbYesNo + MsgBoxStyle.Information, "Remove EOF...")
            If msgSect = vbYes Then
                RemSector = (150 * 2352)
            End If
        End If

        For Each track In tracks
            If track.LBA < 0 Then Continue For
            Dim trackLength As Integer = track.NextTrack.LBA - track.LBA
            Dim startLba As Integer = track.LBA
            Dim ExtTrack As String

            dsr.Policy.DeterministicClearBuffer = False

            If track.IsAudio Then
                ExtTrack = ".raw"
            ElseIf track.IsData Then
                ExtTrack = ".iso"
            End If

            'RemSector = (2048 * 150) '//Remove empty value from bin data file?//
            '//Could be a solution to extract data in MODE1/2048? - anyway it doesn't work
            'trackdata = New Byte(trackLength * 2048 - 1) {}
            'For sector As Integer = 0 To trackLength - 1
            'dsr.ReadLBA_2048(startLba + sector, trackdata, sector * 2048)
            'Next

            TrackData = New Byte(trackLength * 2352 - 1) {}
            For sector As Integer = 0 To trackLength - 1
                dsr.ReadLBA_2352(startLba + sector, TrackData, sector * 2352)
            Next

            Dim TrackPath As String = String.Format("{0} {1:D2}" & ExtTrack, Path.Combine(Dpath, filebase), track.Number)

            If File.Exists(TrackPath) Then

                If Not confirmed Then
                    Dim dr = MessageBox.Show("This file already exists. Do you want extraction to proceed overwriting files, or cancel the entire operation immediately?", "File already exists", MessageBoxButtons.OKCancel)
                    If dr = DialogResult.Cancel Then Return
                    confirmed = True
                End If

                File.Delete(TrackPath)
            End If

            Dim tempfile As String = Replace(Path.GetTempFileName(), ".tmp", ExtTrack)

            Try
                '//Write all flux without control
                'File.WriteAllBytes(tempfile, TrackData)

                Dim fs As FileStream
                fs = New FileStream(tempfile, FileMode.Create)
                fs.Write(TrackData, 0, TrackData.Length - RemSector)
                fs.Close()

                'convert raw to wav
                If ExtTrack = ".raw" Then
                    LazyAss.TaskEnd = "Done."
                    LazyAss.wDir = Application.StartupPath & "\Converter"
                    LazyAss.tProcess = Application.StartupPath & "\Converter\sox.exe"
                    LazyAss.Arg = "-r 44100 -e signed-Integer -b 16 -c 2 " & Chr(34) & tempfile & Chr(34) & " " & Chr(34) & Replace(TrackPath, ".raw", ".wav") & Chr(34)
                    LazyAss.StartProcess()
                Else
                    File.Copy(tempfile, TrackPath)
                End If

                LazyAss.LogOut.AppendText(vbCrLf & "-- FILE " & Path.GetFileName(TrackPath) & " EXTRACTED !! --" & vbCrLf)
                LazyAss.LogOut.ScrollToCaret()
            Finally
                File.Delete(tempfile)
            End Try
        Next
        disc2.Dispose()
    End Sub

End Module