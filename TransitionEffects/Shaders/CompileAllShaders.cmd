@echo off

@set FXCTOOL="C:\Program Files\Microsoft DirectX SDK (March 2008)\Utilities\Bin\x86\fxc.exe"
@set BYTECODEDIR=.

call :Compile DropFade.fx
call :Compile SlideIn.fx
call :Compile Fade.fx
call :Compile CircleReveal.fx
call :Compile Blinds.fx
call :Compile LineReveal.fx
call :Compile CloudReveal.fx
call :Compile Blood.fx
call :Compile RandomCircleReveal.fx
call :Compile CircleStretch.fx
call :Compile Water.fx
call :Compile Crumble.fx
call :Compile RotateCrumble.fx
call :Compile Ripple.fx
call :Compile RadialWiggle.fx
call :Compile RadialBlur.fx
call :Compile CircularBlur.fx
call :Compile Disolve.fx
call :Compile Swirl.fx
call :Compile BandedSwirl.fx
call :Compile MostBright.fx
call :Compile LeastBright.fx
call :Compile Saturate.fx
call :Compile Shrink.fx
call :Compile Pixelate.fx
call :Compile PixelateIn.fx
call :Compile PixelateOut.fx
call :Compile SwirlGrid.fx
call :Compile SmoothSwirlGrid.fx
call :Compile Wave.fx

goto Success

:Compile 
        %FXCTOOL% /nologo /T ps_2_0 %1 /E main /Fo%BYTECODEDIR%\%1.ps
        exit/b 0

:Success
        echo Completed.
        exit/b 0
