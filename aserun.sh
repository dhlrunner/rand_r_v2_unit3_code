export EM8XXX_SERVER=":0"
export MRUA_PACKAGE_DIR="/tango/sdk/mrua"
export DFB_CONFIG_DIR=/tango/sdk/dcchd_build/
export MRUAFW_PACKAGE_DIR="/tango/sdk/mruafw"
export LD_LIBRARY_PATH=/tango/sdk/yamaha/lib:$MRUA_PACKAGE_DIR/lib/:$LD_LIBRARY_PATH
export PATH=/tango/sdk/yamaha/bin:$MRUA_PACKAGE_DIR/bin:$MRUA_PACKAGE_DIR/MRUA_src/llad_xtest:$MRUA_PACKAGE_DIR/MRUA_src/rmchannel/xos2k_client:$MRUA_PACKAGE_DIR/MRUA_src/llad_smallapps:$PATH
cd /tango/sdk/dcchd_build
source trun.env
export EMPRESSPATH=/tango/sdk/empress/xtarget/linux-mipsel
export PATH=$EMPRESSPATH/bin:$PATH
export LD_LIBRARY_PATH=$EMPRESSPATH/shlib:$LD_LIBRARY_PATH
hwclock -ls
ulimit -c 9999999
ulimit -s 128

cd /tango/sdk/yamaha/app
/tango/sdk/yamaha/app/Ase