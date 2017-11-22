#! /usr/bin/env gforth

\ Serpentino

\ Description: A simple snake game project written in Forth for Gforth
\ and Solo Forth. Under development.

\ Author: Marcos Cruz (programandala.net)
\ http://programandala.net
\ http://github.com/programandala-net/serpentino 

\ Last modified 201711221228

\ =============================================================
\ License

\ You may do whatever you want with this work, so long as you
\ retain all copyright, credit and authorship notices, and this
\ license.  There is no warranty.

\ =============================================================
\ Credit

\ Original code by Robert Pfeiffer:
\ <https://github.com/robertpfeiffer/forthsnake>.

\ ==============================================================

: not ( x1 -- x2 ) true xor ;

: myrand ( n1 n2 -- n3 ) over - utime + swap mod + ;

: snake-size 200 ;

: xdim 50 ;

: ydim 20 ;

create snake snake-size cells 2 * allot

create apple 2 cells allot

variable head

variable length

variable direction

: segment ( seg -- adr ) head @ + snake-size mod cells 2 * snake + ;

: pos+ ( x1 y1 x2 y2 -- x y ) rot + -rot + swap ;

: point= 2@ rot 2@ rot = -rot = and ;

: head* ( -- x y ) 0 segment ;

: move-head! ( -- ) head @ 1 - snake-size mod head ! ;

: grow! ( -- ) 1 length +! ;

: eat-apple ( -- )  1 xdim myrand 1 ydim myrand apple 2! grow! ;

: step! ( xdiff ydiff -- ) head* 2@ move-head! pos+ head* 2! ;

: left  ( -- n1 n2 ) -1  0 ;

: right ( -- n1 n2 )  1  0 ;

: down  ( -- n1 n2 )  0  1 ;

: up    ( -- n1 n2 )  0 -1 ;

: wall? ( -- f )
  head* 2@ 1 ydim within swap 1 xdim within and not ;

: crossing? ( -- f )
  false length @ 1 ?do i segment head* point= or loop ;

: apple? ( -- f ) head* apple point= ;

: dead? wall? crossing? or ;

: draw-frame ( -- )
  0 0 at-xy xdim 0 ?do ." +" loop
            ydim 0 ?do xdim i at-xy ." +" cr ." +" loop
            xdim 0 ?do ." +" loop cr ;

: draw-snake ( -- )
  length @ 0 ?do i segment 2@ at-xy ." #" loop ;

: draw-apple ( -- ) apple 2@ at-xy ." Q" ;

: render ( -- )
  page draw-snake draw-apple draw-frame cr length @ . ;

: init ( -- )
  0 head !
  xdim 2 / ydim 2 / snake 2!
  3 3 apple 2!
  3 length !
  ['] up direction !
  left step! left step! left step! left step! ;

: (rudder) ( -- xt )
  key case
    'a' of ['] left  endof
    'w' of ['] up    endof
    'd' of ['] right endof
    's' of ['] down  endof
    direction @ swap
  endcase ;

: rudder ( -- n1 n2 )
  key? if (rudder) direction ! then direction perform ;

: (game) ( n -- )
  render ms rudder step! apple? if eat-apple then ;

: game-over ( -- ) ." *** GAME OVER ***" ;

: game ( n -- ) begin dup (game) dead? until drop ;

init

page
." Serpentino"
3000 ms
200 game

\ =============================================================
\ Change log

\ 2017-11-22: Fork from Robert Pfeiffer's forthsnake
\ (https://github.com/robertpfeiffer/forthsnake). Change source style.
\ Rename words. Factor the main loop.
