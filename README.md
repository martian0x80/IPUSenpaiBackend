<!-- Author:  (martian0x80) -->
<!-- Last Modified: 2024-05-18 -->

<div align="center">

<h1 style="border-bottom: none">
    <b><a href="https://www.ipusenpai.in/">IPU Senpai</a></b>
    <br>
</h1>
<a href="assets/logo.png">
    <img alt="ipusenpai logo" src="assets/logo.png" style="width: 25%; height: auto; max-width: 300px; max-height: 300px">
</a>
<br/>
<br/>

[![IPUSenpai Backend](https://github.com/martian0x80/IPUSenpaiBackend/actions/workflows/docker-workflow.yml/badge.svg)](https://github.com/martian0x80/IPUSenpaiBackend/actions/workflows/docker-workflow.yml)

[![Uptime Status](https://status.rmrf.online/api/badge/11/status?style=flat)](https://www.rmrf.online/status/ipusenpai)
[![Uptime Status](https://status.rmrf.online/api/badge/11/ping?style=flat)](https://www.rmrf.online/status/ipusenpai)
[![Uptime Status](https://status.rmrf.online/api/badge/11/uptime?style=flat)](https://www.rmrf.online/status/ipusenpai)
[![Uptime Status](https://status.rmrf.online/api/badge/11/response?style=flat)](https://www.rmrf.online/status/ipusenpai)

[![GitHub issues](https://img.shields.io/github/issues/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/issues)
[![GitHub license](https://img.shields.io/github/license/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/blob/master/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/commits/master)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/pulls)

[![GitHub forks](https://img.shields.io/github/forks/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/network)
[![GitHub stars](https://img.shields.io/github/stars/martian0x80/IPUSenpaiBackend)](https://github.com/martian0x80/IPUSenpaiBackend/stargazers)

<p align="center">
  A modern, open-source, beautifully designed, ready-to-use alternative to ipuranklist for IPU students. Built with Next.js, Tailwind CSS, and TypeScript.
</p>
<a href="assets/landing.png">
    <img alt="ipusenpai landing" src="assets/landing.png">
</a>
</div>

<br/>

<div align="center">
    <a href="https://www.ipusenpai.in/">Home Page</a> |
    <a href="">Discord</a> |
    <a href="mailto:ipusenpai0x80@gmail.com">Mail</a> |
    <a href="https://github.com/martian0x80/IPUSenpaiBackend/">Backend Repository</a>
</div>
<br/>

<div align="center">
    <p style="font-size: 2em; font-weight: bold">
        Architecture
    </p>

<a href="assets/arch.jpg">
    <img alt="ipusenpai architecture" src="assets/arch.jpg" style="width: 100%; height: auto; max-width: 1000px; max-height: 1000px">
</a>
</div>


## Overview

This is the backend for the IPUSenpai project.

The frontend for this project can be found [here](https://devel.ipusenpai.in/).

## Brief Overview
- The backend is built using ASP.NET Core and ~Entity Framework Core~ Dapper. It is hosted on Azure and uses Azure Postgresql Database for data storage. (~Will be moved to my VPS after I run out of Azure Student Sponsorship balance~ Weeell, I'm out of Azure credits now.)
- The API uses Redis for caching. This is to reduce the number of database queries and improve performance.
- The API uses Brotli and Gzip compression to reduce the size of the response body.

Here's a peek of the student dashboard:
![Student Dashboard](https://github.com/martian0x80/IPUSenpaiBackend/blob/master/assets/dashboard.png)

## Like My Work?
- If you like my work, you can star the repository.

<img src="assets/star.png" style="width: 100%"/>

> [!NOTE]
> This project is still in development and is not yet ready for production.

## Issues
- Report issues [here](https://github.com/martian0x80/IPUSenpaiBackend/issues).
- Don't report any issues if they are already known or listed. Just simply react.
