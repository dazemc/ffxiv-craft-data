#! /bin/bash
git submodule update --init --recursive
git submodule foreach git pull origin master
git add .
git commit -m "updated submodules"
git push
exit
