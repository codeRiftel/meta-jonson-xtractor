.PHONY: build test

build:
	mcs main.cs X.cs VJP.cs option/*cs -out:xtractor.exe
