.PHONY: build test

build:
	mcs main.cs X.cs Jonson.cs option/*cs -out:xtractor.exe
