#!/bin/bash
#
#
#       Sets up a dev env with all pre-reqs. This script is idempotent, it will
#       only attempt to install dependencies, if not exists.   
#
# ---------------------------------------------------------------------------------------
#

set -e
set -m

export NODE_VERSION=22.16.0
export NVM_VERSION=v0.40.3
export GO_VERSION=1.22.0

echo ""
echo "┌────────────────┐"
echo "│ Installing NVM │"
echo "└────────────────┘"
echo ""

# nvm doesn't show up without sourcing this script. This is done in bashrc,
# but does not stick in the subshell for scripts.
#
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"  # This loads nvm
[ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"  # This loads nvm bash_completion
if ! command -v nvm &> /dev/null; then
    echo "nvm not found, installing..."
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/${NVM_VERSION}/install.sh 2>&1 | bash
    # Load nvm again after install
    [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"  # This loads nvm
    [ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"  # This loads nvm bash_completion
else
    echo "nvm already installed"
fi

echo ""
echo "┌────────────────────────────────────┐"
echo "│ Checking for language dependencies │"
echo "└────────────────────────────────────┘"
echo ""

if ! command -v go &> /dev/null; then
    GO_DOWNLOAD_DIR=`mktemp -d`
    wget -o- "https://go.dev/dl/go${GO_VERSION}.linux-amd64.tar.gz" -P $GO_DOWNLOAD_DIR
    sudo rm -rf /usr/local/go && sudo tar -C /usr/local -xzf $GO_DOWNLOAD_DIR/go${GO_VERSION}.linux-amd64.tar.gz

    USER_HOME=$(eval echo ~$USER)
    LINES="export PATH=\$PATH:/usr/local/go/bin:$USER_HOME/go/bin"
    echo -e "$LINES" >> ~/.bashrc
    source ~/.bashrc
fi

/usr/local/go/bin/go install github.com/go-delve/delve/cmd/dlv@latest
/usr/local/go/bin/go install github.com/magefile/mage@latest

echo ""
echo "┌─────────────────┐"
echo "│ Installing Node │"
echo "└─────────────────┘"
echo ""

nvm install $NODE_VERSION

echo ""
echo "┌──────────────────────┐"
echo "│ Installing CLI tools │"
echo "└──────────────────────┘"
echo ""

if ! command -v docker &> /dev/null; then
    echo "docker not found - installing..."
    curl -sL https://get.docker.com | sudo bash
fi
sudo chmod 666 /var/run/docker.sock

echo ""
echo "┌──────────────────────────┐"
echo "│ Installing site packages │"
echo "└──────────────────────────┘"
echo ""

npm config set registry https://registry.npmjs.org/
sudo apt-get update
sudo apt-get install libnss3 libnspr4 libasound2t64 -y
npx playwright install-deps
npx playwright install

echo ""
echo "┌───────────────────────────────┐"
echo "│ Installing VS Code extensions │"
echo "└───────────────────────────────┘"
echo ""

code --install-extension github.copilot
code --install-extension eamodio.gitlens
code --install-extension golang.go@0.45.0
code --install-extension ms-vscode.makefile-tools

echo ""
echo "┌──────────┐"
echo "│ Versions │"
echo "└──────────┘"
echo ""

echo "Docker: $(docker --version)"
echo "Go: $(/usr/local/go/bin/go version)"
echo "Mage version: " $(/home/boor/go/bin/mage --version)
echo "npm version: " $(npm --version)
echo "npx version: " $(npx --version)
echo "nvm version: " $(nvm --version)