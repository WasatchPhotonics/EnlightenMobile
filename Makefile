all:
	@echo "Please build project from Visual Studio"

clean:
	@rm -rf EnlightenMobile/{bin,obj}         \
            EnlightenMobile.UITests/{bin,obj} \
            EnlightenMobile.Android/{bin,obj} \
            EnlightenMobile.iOS/{bin,obj}     
