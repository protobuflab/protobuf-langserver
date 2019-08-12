echo ""
echo "----------- Restoring and compiling vscode-extension -----------"
echo ""
pushd vscode-extension

if [ -f node_modules ]
then
    rm -rf node_modules
fi

if [ -f package-lock.json ]
then
    rm package-lock.json
fi

npm install
npm run compile

if [ $? -ne 0 ];
then
    popd
    echo ""
    echo "vscode-extension build failed!"
    exit 1
fi
popd

echo ""
echo "----------- Building language-server -----------"
echo ""
pushd language-server
rm -rf node_modules
rm package-lock.json
npm install
npm run compile

if [ $? -ne 0 ];
then
    popd
    echo ""
    echo "language server build failed!"
    exit 1
fi
popd